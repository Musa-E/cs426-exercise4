// add score manager
using UnityEngine;
using UnityEngine.UI;

// access the Text Mesh Pro namespace
using TMPro;

public class InventoryManager : MonoBehaviour
{


    // The header that says "Inventory:"
    public TMP_Text inventoryTitle;

    // Each inventory slot (max of 3)
    public TMP_Text slot1;
    public TMP_Text slot2;
    public TMP_Text slot3;
    public TMP_Text slot4;
    public TMP_Text slot5;
    public TMP_Text slot6;
    public TMP_Text slot7;
    public TMP_Text slot8;
    public TMP_Text slot9;
    public TMP_Text slot10;
    public TMP_Text slot11;


    // Start is called before the first frame update
    void Start()
    {

        inventoryTitle.text = "<b>Inventory:</b>"; // '\u2612' -> '\u2611' 
        slot1.text = "[x] Keyboard";
        slot2.text = "[x] Mouse";
        slot3.text = "[x] Scanner";
        slot4.text = "[x] Joystick";
        slot5.text = "[x] Control Unit";
        slot6.text = "[x] ALU";
        slot7.text = "[x] Memory";
        slot8.text = "[x] Monitor";
        slot9.text = "[x] Printer";
        slot10.text = "[x] Speaker";
        slot11.text = "[x] Headphones";
    }

    //we will call this method from our target script
    // whenever the player collides or shoots a target a point will be added
    public void turnedIn(Part turnedInPart)
    {
        if (turnedInPart == null) {
            // turnedInPart.WasTurnedIn = true;
        }

        // Update the UI accordingly
        switch(turnedInPart.Name) {

            // Input Devices
            case("Keyboard"):
                slot1.text = "[√] <s>Keyboard</s>";
                return;
            case("Mouse"):
                slot2.text = "[√] <s>Mouse</s>";
                return;
            case("Scanner"):
                slot3.text = "[√] <s>Scanner</s>";
                return;
            case("Joystick"):
                slot4.text = "[√] <s>Joystick</s>";
                return;
            
            // CPU devices
            case("Control Unit"):
                slot5.text = "[√] <s>Control Unit</s>";
                return;
            case("ALU"):
                slot6.text = "[√] <s>ALU</s>";
                return;
            case("Memory"):
                slot7.text = "[√] <s>Memory</s>";
                return;

            // Output devices
            case("Monitor"):
                slot8.text = "[√] <s>Monitor</s>";
                return;
            case("Printer"):
                slot9.text = "[√] <s>Printer</s>";
                return;
            case("Speaker"):
                slot10.text = "[√] <s>Speaker</s>";
                return;
            case("Headphones"):
                slot11.text = "[√] <s>Headphones</s>";
                return;
            
            case(null): // Just in case
                return;
            
            default: // Just in case
                Debug.Log("An error occured while trying to update the inventory UI: Invalid/Unexpected part name");
                return;
        }

    }
}