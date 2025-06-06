using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class SceneScript : NetworkBehaviour
{
    public TMPro.TextMeshProUGUI canvasStatusText;
    public PlayerManager plMove;
    //Para la lógica de meter tu nombre:
    //public InputField enterName;

    [SyncVar(hook = nameof(OnStatusTextChanged))]
    public string statusText;

    void OnStatusTextChanged(string _Old, string _New)
    {
        //called from sync var hook, to update info on screen for all players
        canvasStatusText.text = statusText;
    }

}
