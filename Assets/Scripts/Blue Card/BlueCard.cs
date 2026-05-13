using UnityEngine;
using UnityEngine.UIElements;

abstract public class BlueCard : ScriptableObject
{
    public string cardName;
    public Sprite image;

    abstract public UIDocument GetUiDocument();
}
