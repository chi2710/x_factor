using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Item", order = 0)]
public class Item : ScriptableObject
{
    public Sprite icon;
    public Sprite iconSelected;
    public string title;
    public string location;
    public string description;
}
