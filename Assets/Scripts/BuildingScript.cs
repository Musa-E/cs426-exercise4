using UnityEngine;
using TMPro;

public class BuildingScript : MonoBehaviour 
{
    public Material activeMat;
    public Material idleMat;
    bool isActive = true;
    public float coolDownTime = 10;
    [SerializeField] private TMP_Text ItemList;

    void start()
    {
        GetComponent<Renderer>().material = activeMat;
    }


    void Update(){
        if(!isActive && coolDownTime > 0){
            coolDownTime -= Time.deltaTime;
            Debug.Log(coolDownTime);
        }
        if(!isActive && coolDownTime <= 0)
        {
            coolDownTime = 10;
            GetComponent<Renderer>().material = activeMat;
            isActive = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("hit detected");
        if (isActive){
            GetComponent<Renderer>().material = idleMat;
            ItemList.text += '\n' + this.name;

            isActive = false;
        }
    }

}
