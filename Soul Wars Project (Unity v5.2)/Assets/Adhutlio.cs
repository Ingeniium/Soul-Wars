using UnityEngine;
using UnityEngine.Networking;
    class Adhutlio : NetworkBehaviour
    {
        [ServerCallback]
        void Start()
        {
            AIController AI = GetComponentInChildren<AIController>();
        ModDisplay display = GetComponentInParent<ModDisplay>();
        display.Mods.Add("<color=red>Boss: Adhutlio</color>");
            AI.StartCoroutine(AI.SetState(AIController.Type.Conquer,Vector3.zero));
        //AI.StartCoroutine(AI.PaintShell(Color.black));
            Flurry flurry = GetComponentInChildren<Flurry>();
            Blaster blaster = GetComponentInChildren<Blaster>();
        string Layer = LayerMask.LayerToName(gameObject.layer);
        flurry.SetBaseStats(Layer);
        blaster.SetBaseStats(Layer);
            flurry.level = 3;
            blaster.level = 3;
            flurry.AddAbility(blaster.GetGunModAbility(5));
            flurry.AddAbility(4);
            flurry.AddAbility(7);
            blaster.AddAbility(5);
            blaster.AddAbility(9);
            blaster.AddAbility(flurry.GetGunModAbility(6));
            
        }

        
    }               

