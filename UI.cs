using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;

namespace BigHeadMode
{
    internal static class UI
    {
        private static string _modName = "BigHeadMode";
        
        public static void LoadIcons(Assembly asm)
        {
            QuickMenuAPI.PrepareIcon(_modName, "BigHead", asm.GetManifestResourceStream("HeadBig.png"));
            QuickMenuAPI.PrepareIcon(_modName, "SmallHead", asm.GetManifestResourceStream("HeadSmall.png"));
            QuickMenuAPI.PrepareIcon(_modName, "NoHead", asm.GetManifestResourceStream("NoHead.png"));
            QuickMenuAPI.PrepareIcon(_modName, "RestoreHead", asm.GetManifestResourceStream("HeadNormal.png"));

        }

        public static void CreateCategory()
        {
            Category obligitory = QuickMenuAPI.MiscTabPage.AddCategory("Big Head Mode", _modName);
            Button big = obligitory.AddButton("Big Heads", "BigHead", "Make heads big.");
            big.OnPress += Core.BigHead;
            Button small = obligitory.AddButton("Shrink Heads", "SmallHead", "Make heads small.");
            small.OnPress += Core.SmallHead;
            Button gone = obligitory.AddButton("Remove Heads", "NoHead", "Remove heads.");
            gone.OnPress += Core.NoHead;
            Button restore = obligitory.AddButton("Restore Heads", "RestoreHead", "Resotre heads to original size.");
            restore.OnPress += Core.RestoreHeadSize;

        }
    }
}
