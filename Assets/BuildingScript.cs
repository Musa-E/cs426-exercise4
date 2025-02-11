using UnityEngine;
using TMPro;

public class BuildingScript : MonoBehaviour
{
    public Material activeMat;
    public Material idleMat;
    void start(){
        GetComponent<Renderer>().material = activeMat;
    }

    [SerializeField] private TMP_Text ItemList;
    private void OnTriggerEnter(Collider other){
        Debug.Log("hit detected");
        GetComponent<Renderer>().material = idleMat;
        ItemList.text = this.name;

    }

}
