using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class CameraKeyboardController : MonoBehaviour
{
    [SerializeField]
    private float camRotSpeed = 500f;
    [SerializeField]
    private float camDistanceFromStack = 20f;
    [Tooltip("Tag name given to block prefabs")]
    [SerializeField]
    private string blockTagName = "Block";
    [Tooltip("Prefab used to construct an instance of a GlassBlock")]
    [SerializeField]
    private GameObject glassBlockPrefab;
    [SerializeField]
    [Tooltip("UI component used to display block info to the player")]
    private TextMeshProUGUI blockInfo;

    private Vector3 camTarget;
    private Vector3 camDirection;
    private Vector3 camPreviousPosition;
    private GameObject[] blocks;
    private GameObject[] stackCenters;
    private int stackIndex;
    private float groundHeight = 0.5f;
    //Used for detecting which block the player has clicked on
    private RaycastHit vision;
    private int rayLength = 1000000;

    //Note: Start() is used here in order to run after GameController.cs' Awake() method is called
    void Start()
    {
        //Find all GameObjects in the scene with blockTagName (i.e. all block instances)
        blocks = GameObject.FindGameObjectsWithTag(blockTagName);
        //Before player gains control and once all Jenga stacks have been constructed (by GameController.cs)...
        //...set camera target to first stack...
        SetCameraTarget(stackCenters.First().transform.position);
        //...and set camera starting position/rotation
        camPreviousPosition = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        SetCameraRotation();
        stackIndex = 0;
    }

    void Update()
    {
        //Left-click + move mouse to orbit camera around current stack focal point
        if (Input.GetMouseButtonDown(0))
        {
            camPreviousPosition = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(0))
        {
            SetCameraRotation();
        }

        //Right-click to display block stats on screen
        if (Input.GetMouseButtonUp(1))
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out vision, rayLength))
            {
                //If the point the player clicked on is a block...
                if (vision.collider.CompareTag(blockTagName))
                {
                    //...use API call info saved to each block to display its stats on screen using TextMeshProUGUI blockInfo
                    StudentStat stat = vision.collider.gameObject.GetComponent<StudentStat>();
                    blockInfo.SetText(stat.Grade +": "+ stat.Domain +"\n"+ stat.Cluster +"\n"+ stat.StandardId +": "+ stat.StandardDescription);
                }
            }
        }

        //Press Space Bar to toggle glass blocks/gravity (1st press) or reload scene (2nd press)
        if(Input.GetKeyUp(KeyCode.Space) && blocks.Length > 0)
        {
            //If gravity is not enabled, this must be the first space bar press
            if(!blocks[0].GetComponent<Rigidbody>().useGravity) { 
                EnableBlockGravity();
            }
            //If gravity is enabled, this must be the 2nd space bar press
            else
            {
                ReloadScene();
            }
        }

        //Press Q to switch Jenga stack focus to previous stack
        if (Input.GetKeyUp(KeyCode.Q))
        {
            stackIndex--;
            //If already on the first stack, cycle back around to last stack
            if (stackIndex < 0)
            {
                stackIndex = stackCenters.Length - 1;
            }
            //Update camera target, position, and rotation
            SetCameraTarget(stackCenters[stackIndex].transform.position);
            SetCameraRotation();
        }
        //Press E to switch Jenga stack focus to next stack
        else if (Input.GetKeyUp(KeyCode.E))
        {
            stackIndex++;
            //If already on the last stack, cycle back around to first stack
            if (stackIndex > stackCenters.Length - 1)
            {
                stackIndex = 0;
            }
            //Update camera target, position, and rotation
            SetCameraTarget(stackCenters[stackIndex].transform.position);
            SetCameraRotation();
        }
    }

    //Set camera target based on pos argument value
    //  Created as a method to facilitate future changes, even though the logic is simple now
    private void SetCameraTarget(Vector3 pos)
    {
        camTarget = pos;
    }

    //Set camera position based on pos argument value
    //  Created as a method to facilitate future changes, even though the logic is simple now
    private void SetCameraPosition(Vector3 pos)
    {
        Camera.main.transform.position = pos;
    }

    //Set camera rotation based on previous camera position and mouse position
    private void SetCameraRotation()
    {
        camDirection = camPreviousPosition - Camera.main.ScreenToViewportPoint(Input.mousePosition);

        //Move camera to target, perform rotation, and then move camera back out to its appropriate distance
        //  This allows for the camera to orbit around its target
        SetCameraPosition(camTarget);
        Camera.main.transform.Rotate(new Vector3(1, 0, 0), camDirection.y * 180);
        Camera.main.transform.Rotate(new Vector3(0, 1, 0), -camDirection.x * 180, Space.World);
        Camera.main.transform.Translate(new Vector3(0, 0, -camDistanceFromStack));

        //Do not allow the camera to move below groundHeight (which would put the camera in the ground)
        if (Camera.main.transform.position.y <= groundHeight)
        {
            SetCameraPosition(new Vector3(Camera.main.transform.position.x, groundHeight, Camera.main.transform.position.z));
        }

        camPreviousPosition = Camera.main.ScreenToViewportPoint(Input.mousePosition);
    }

    //Reload the current scene
    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    //Remove GlassBlock instances and enable gravity
    private void EnableBlockGravity()
    {
        foreach (GameObject block in blocks)
        {
            if (block.name.Contains(glassBlockPrefab.name))
            {
                Destroy(block);
            }
            else
            {
                block.GetComponent<Rigidbody>().useGravity = true;
            }
        }
    }

    //Used to pass in List of stackCenters from another script (i.e. GameController.cs)
    public void SetStackCenters(List<GameObject> stackCenters)
    {
        this.stackCenters = stackCenters.ToArray();
    }
}
 