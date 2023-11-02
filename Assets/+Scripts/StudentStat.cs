
using UnityEngine;

//StudentStat API Object used to store block information
public class StudentStat : MonoBehaviour
{
    public int Id { get; set; }
    public string Subject { get; set; }
    public string Grade { get; set; }
    public int Mastery { get; set; }
    public string DomainId { get; set; }
    public string Domain { get; set; }
    public string Cluster { get; set; }
    public string StandardId { get; set; }
    public string StandardDescription { get; set; }

    //Define Equals method to allow for custom equality comparison between StudentStat objects
    public bool Equals(GameObject obj)
    {
        var other = obj.GetComponent<StudentStat>() as StudentStat;

        if (other == null) 
            return false;
        //Stats are identical if all properties are the same except for Id and Mastery
        if (Subject != other.Subject || Grade != other.Grade 
                || DomainId != other.DomainId || Domain != other.Domain 
                || Cluster != other.Cluster || StandardId != other.StandardId 
                || StandardDescription != other.StandardDescription)
            return false;
        return true;
    }
}
