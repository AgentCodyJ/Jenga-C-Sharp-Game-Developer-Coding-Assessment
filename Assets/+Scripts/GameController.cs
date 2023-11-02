using UnityEngine;
using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;

public class GameController : MonoBehaviour
{
    [Tooltip("Prefab used to construct an instance of a GlassBlock")]
    [SerializeField]
    private GameObject glassBlockPrefab;
    [Tooltip("Prefab used to construct an instance of a WoodBlock")]
    [SerializeField]
    private GameObject woodBlockPrefab;
    [Tooltip("Prefab used to construct an instance of a StoneBlock")]
    [SerializeField]
    private GameObject stoneBlockPrefab;
    [Tooltip("Prefab used to construct an instance of a Jenga stack label")]
    [SerializeField]
    private GameObject stackLblPrefab;
    [SerializeField]
    private float blockHeight = 1.1f;
    [SerializeField]
    private float blockWidth = 1.1f;
    [SerializeField]
    private Vector3 firstStackPos = new Vector3(0f,0.55f,0f);
    [SerializeField]
    private float distanceBetweenStacks = 15f;

    private string apiURL = "https://ga1vqcu3o1.execute-api.us-east-1.amazonaws.com/Assessment/stack";
    private Quaternion firstStackRot = Quaternion.Euler(0f,90f,0f);
    private IEnumerable<IGrouping<string, StudentStat>> gradeStacks;
    private IEnumerable<StudentStat> studentStats;
    private List<GameObject> stackCenters = new List<GameObject>();
    private int blocksPerRow = 3;
    private int blockRowCount = 1;
    private int currentGrade;
    private int statGrade;
    private Vector3 currentStackPos;
    private Vector3 blockPos;
    private Quaternion blockRot;
    private bool blockRotated = false;
    private GameObject currentBlock = null;
    private GameObject previousBlock = null;
    private GameObject blockPrefab;
    private bool stackEmpty;

    // Fetch student data to be used in constructing each of the three Jenga stacks
    //  Note: This is done once at the start of the game
    //  (making the assumption the data will not change in the middle of gameplay)
    //Note: Awake() is used here in order to load in the blocks before CameraKeyboardController.cs' Start() method is called
    void Awake()
    {
        using (var webClient = new WebClient())
        {
            studentStats = JsonConvert.DeserializeObject<IEnumerable<StudentStat>>(webClient.DownloadString(apiURL));
            if(studentStats.Count() == 0)
            {
                Debug.LogError("API Call returned no records");
                return;
            }
        }

        //Use LINQ to order stats by grade, then by domain name ascending, then by cluster name ascending, then by standard ID ascending
        //  Once ordered, group stats by grade to make building stacks easier
        gradeStacks = studentStats.OrderBy(x => x.Grade).ThenBy(x => x.Domain)
                                  .ThenBy(x => x.Cluster).ThenBy(x => x.StandardId)
                                  .GroupBy(x => x.Grade);

        //Set initial stack variables
        currentStackPos = firstStackPos;
        blockPos = currentStackPos;
        blockRot = firstStackRot;
        stackEmpty = true;
        //Calculate currentGrade from first StudentStat record
        currentGrade = Int32.Parse(gradeStacks.First().Key.Substring(0, 1));

        foreach (IGrouping<string, StudentStat> grade in gradeStacks)
        {
            //Determine if a new Jenga stack should be started
            try
            {
                //Create stack camera focal point for previous stack (not used on first loop execution)
                if (!stackEmpty)
                {
                    CreateStackCenter();
                }

                //Determine the next grade level
                //Note: Assumes data value for "Grade" starts with a single digit number
                //  This could be extended for double digit numbers or Kindergarten, but for the this example it is restricted to grades 1-9
                statGrade = Int32.Parse(grade.Key.Substring(0, 1));
                if (statGrade > currentGrade && !stackEmpty)
                {
                    currentGrade = statGrade;

                    //Increment the current stack position to start building the next stack
                    //  This allows for support beyond just three grade stacks without needing a code change
                    currentStackPos.x += distanceBetweenStacks;
                    blockPos = currentStackPos;
                    //Rotate blocks back to starting rotation
                    if(blockRotated)
                    {
                        blockRot *= Quaternion.Euler(0, 90, 0);
                        blockRotated = false;
                    }
                    blockRowCount = 1;
                    stackEmpty = true;
                }
            }
            catch (Exception)
            {
                stackEmpty = true;
                //Proceed to next stat, as data containing an invalid grade has been found and should be skipped
                continue;
            }

            foreach (StudentStat stat in grade)
            {
                //Check for duplicate stats
                if (previousBlock != null && stat.Equals(previousBlock))
                {
                    //If duplicate is found, update the existing block with the most recent duplicate's Mastery stat and skip creating the duplicate block
                    //  NOTE: This logic may be updated to simply ignore the duplicate data or handle it in another way
                    previousBlock.GetComponent<StudentStat>().Mastery = stat.Mastery;
                    continue;
                }

                //Determine block type
                switch (stat.Mastery)
                {
                    case 0:
                        blockPrefab = glassBlockPrefab;
                        break;
                    case 1:
                        blockPrefab = woodBlockPrefab;
                        break;
                    case 2:
                        blockPrefab = stoneBlockPrefab;
                        break;
                    default:
                        //Proceed to next stat, as data containing an invalid mastery has been found and should be skipped
                        continue;
                }

                //Create block
                currentBlock = Instantiate(blockPrefab, blockPos, blockRot);
                //Assign newly created block's StudentStat variables from API call data
                StudentStat currentStat = currentBlock.GetComponent<StudentStat>();
                currentStat.Id = stat.Id;
                currentStat.Subject = stat.Subject;
                currentStat.Grade = stat.Grade;
                currentStat.Mastery = stat.Mastery;
                currentStat.DomainId = stat.DomainId;
                currentStat.Domain = stat.Domain;
                currentStat.Cluster = stat.Cluster;
                currentStat.StandardId = stat.StandardId;
                currentStat.StandardDescription = stat.StandardDescription;
                previousBlock = currentBlock;

                //If this is the first block created for the stack also create the stack label
                if(stackEmpty)
                {
                    GameObject stacklbl = Instantiate(stackLblPrefab, new Vector3(blockPos.x + blockWidth, blockPos.y + blockHeight*2, blockPos.z - blockHeight*3), Quaternion.identity);
                    stacklbl.GetComponent<TextMeshPro>().text = (currentStat.Grade);
                    stackEmpty = false;
                }

                //Compute position and rotation of next block
                //  If needed, handle creating new row based upon blocksPerRow
                if (blockRowCount >= blocksPerRow)
                {
                    //Start row over by resetting block row count
                    blockRowCount = 1;

                    //Rotate block by 90 degrees, which will alternate row direction
                    blockRot *= Quaternion.Euler(0,90,0);
                    blockRotated = !blockRotated;

                    //Move next blockPos up to next row
                    //Prepare next row position based on whether row is rotated or not
                    if(!blockRotated)
                    {
                        blockPos.Set(currentStackPos.x, blockPos.y + blockHeight, currentStackPos.z);
                    } 
                    else
                    {
                        blockPos.Set(currentStackPos.x + blockWidth, blockPos.y + blockHeight, currentStackPos.z - blockWidth);
                    }
                }
                else
                {
                    if(!blockRotated)
                    {
                        //Move next block x pos in same row
                        blockPos.x += blockWidth;
                    } 
                    else
                    {
                        //Move next block z pos in same row
                        blockPos.z += blockWidth;
                    }
                    //Increment block row count
                    blockRowCount++;
                }

                //Log debug data to Unity console
                Debug.Log(stat.Id + " - " + grade.Key + ", " + stat.Domain + ", " + stat.Cluster + ", " + stat.StandardId);
            }
        }

        //Create final stack camera focal point, if the stack is not empty
        if(!stackEmpty)
        {
            CreateStackCenter();
        }
        //Send stackCenters to CameraKeyboardController script
        GetComponent<CameraKeyboardController>().SetStackCenters(stackCenters);
    }

    //Create a focal point for the rotating camera at the halfway point of the stack (ie the spot the camera focuses on for each stack)
    //  This allows for runtime generation of each stack's camera focal point, rather than relying on a static position that would
    //  need to be updated if the # of blocks per stack or # of stacks ever changes in the future
    private void CreateStackCenter()
    {
        stackCenters.Add(Instantiate(new GameObject(), new Vector3(currentStackPos.x, blockPos.y / 2, currentStackPos.z), blockRot));
    }
}
