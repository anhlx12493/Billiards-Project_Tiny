using System;
using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Tiny.UI;

public class ClickController : SystemBase
{

    [DllImport("__Internal")]
    private static extern void OpenStore();

    protected override void OnUpdate()
    {
        ClickAds();
    }

    private void ClickAds()
    {
        Entity buttonClickAds = World.GetExistingSystem<ProcessUIEvents>().GetEntityByUIName("ButtonPlayNow");
        Entity eClicked = Entity.Null;
        Entities.ForEach((Entity e, in UIState state) => 
        {
            if (state.IsClicked)
            {
                eClicked = e;
            }
        }).Run();
        if(eClicked != null)
        {
            if(eClicked == buttonClickAds)
            {
                OpenStore();
            }
        }
    }
}
