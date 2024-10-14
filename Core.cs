using MelonLoader;
using ABI_RC.Core.Player;
using UnityEngine;

[assembly: MelonInfo(typeof(BigHeadMode.Core), "BigHeadMode", "1.0.0", "JillTheSomething", null)]
[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]

namespace BigHeadMode
{
    public class Core : MelonMod
    {
        public static Dictionary<string, Vector3> OriginalHeadScales = new Dictionary<string, Vector3>();
        public static Dictionary<string, (GameObject, float)> LateScaleQueue = new Dictionary<string, (GameObject, float)>();

        private static MelonPreferences_Category BigHeadModePrefrenceCategory;
        private static MelonPreferences_Entry<bool> ScaleLocal;
        private static MelonPreferences_Entry<bool> ScaleRemote;
        private static MelonPreferences_Entry<float> Multipler;


        public override void OnInitializeMelon()
        {
            UI.CreateCategory();
            BigHeadModePrefrenceCategory = MelonPreferences.CreateCategory("BigHeadMode");
            ScaleLocal = MelonPreferences.CreateEntry<bool>("BigHeadMode", "ScaleLocal", true, "Scale Local Player", "Scale the local player head.");
            ScaleRemote = MelonPreferences.CreateEntry<bool>("BigHeadMode", "ScaleRemote", true, "Scale Remote Player", "Scale remote players head.");
            Multipler = MelonPreferences.CreateEntry<float>("BigHeadMode", "Multiplier", 2.0f, "Multiplier", "What to grow/shrink the head by (shrinks using `headScale * 1/multiplier`.");

            UI.LoadIcons(MelonAssembly.Assembly);
            LoggerInstance.Msg("BigHeadMode Initialized.");
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (LateScaleQueue.Count > 0)
            {

                List<string> toClear = new List<string>();
                foreach (string uuid in LateScaleQueue.Keys)
                {
                    if (LateScaleQueue[uuid].Item1.transform != null && LateScaleQueue[uuid].Item1.transform.GetChild(2).childCount != 0)
                    {
                        //MelonLogger.Msg($"Late Scaling uuid {uuid}");
                        GameObject avatar = LateScaleQueue[uuid].Item1.transform.GetChild(2).GetChild(0).gameObject;
                        Animator animator = avatar.GetComponent<Animator>();
                        Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);
                        SetHeadSize(headBone, uuid, LateScaleQueue[uuid].Item2);
                        toClear.Add(uuid);
                    }
                }
                foreach (string uuid in toClear) LateScaleQueue.Remove(uuid);
            }
        }

        public static List<CVRPlayerEntity> GetUserlist()
        {
            List<CVRPlayerEntity> playerList = new List<CVRPlayerEntity>();
            if (!(ScaleLocal.Value || ScaleRemote.Value))
            {
                MelonLogger.Warning("ScaleLocal and ScaleRemote are both set to false. Set at least one to true if you want to grow/shrink any heads!");
            }
            if (ScaleRemote.Value)
            {
                foreach (CVRPlayerEntity p in CVRPlayerManager.Instance.NetworkPlayers)
                {
                    playerList.Add(p);
                }
            }
            return playerList;
        }

        public static void SetAllHeadSize(List<CVRPlayerEntity> players, float multiplier)
        {
            LateScaleQueue.Clear();
            foreach (CVRPlayerEntity p in players)
            {
                try
                {
                    //MelonLogger.Msg($"Scaling {p.Username} head by {multiplier}.");
                    if (p.PlayerObject.transform.GetChild(2).childCount != 0) { 
                        GameObject avatar = p.PlayerObject.transform.GetChild(2).GetChild(0).gameObject;
                        Animator animator = avatar.GetComponent<Animator>();
                        Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);
                        SetHeadSize(headBone, p.Uuid, multiplier);
                    }
                    else
                    {
                        LateScaleQueue.Add(p.Uuid,(p.PlayerObject, multiplier));                       
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Error($"  Could not scale:\n  {e}");
                }
            }
            if (ScaleLocal.Value)
            {
                //MelonLogger.Msg($"Scaling local user head by {multiplier}.");
                Transform headBone = PlayerSetup.Instance.animatorManager.Animator.GetBoneTransform(HumanBodyBones.Head);
                SetHeadSize(headBone, "local", multiplier);
            }
        }

        public static void SetHeadSize(Transform headBone, string uuid, float multiplier)
        {
            try
            {
                Vector3 originalScale;
                if (OriginalHeadScales.ContainsKey(uuid)) originalScale = OriginalHeadScales[uuid];
                else
                {
                    originalScale = headBone.localScale;
                    OriginalHeadScales.Add(uuid, headBone.localScale);
                }
                headBone.localScale = new Vector3(originalScale.x * multiplier, originalScale.y * multiplier, originalScale.z * multiplier);
            }
            catch//(Exception e)
            {
                //MelonLogger.Error($"SetHeadSize Error:\n  {e}\n[{headBone} {uuid} {multiplier}]");
            }
        }

        public static void RestoreHeadSize()
        {
            List<string> toClear = new List<string>();
            LateScaleQueue.Clear();
            foreach (string k in OriginalHeadScales.Keys)
            {
                if(k == "local")
                {
                    Transform headBone = PlayerSetup.Instance.animatorManager.Animator.GetBoneTransform(HumanBodyBones.Head);
                    headBone.localScale = OriginalHeadScales["local"];
                    toClear.Add("local");
                    continue;
                }
                try
                {
                    CVRPlayerEntity p = null;
                    foreach (CVRPlayerEntity pe in CVRPlayerManager.Instance.NetworkPlayers)
                    {
                        if (pe.Uuid == k && pe.PlayerObject != null) p = pe;
                    }
                    if (p == null) continue;
                    toClear.Add(k);
                    //MelonLogger.Msg($"Restoring {p.Username} head.");
                    GameObject avatar = p.PlayerObject.transform.GetChild(2).GetChild(0).gameObject;
                    Animator animator = avatar.GetComponent<Animator>();
                    Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);
                    headBone.localScale = OriginalHeadScales[k];
                }
                catch //(Exception e)
                {
                    //MelonLogger.Warning($"RestoreHeadSize failed:\n{e}");
                }
            }
            foreach (string k in toClear) OriginalHeadScales.Remove(k);
        }

        public static void BigHead()
        {
            List<CVRPlayerEntity> pl = GetUserlist();
            SetAllHeadSize(pl, Multipler.Value);
        }

        public static void SmallHead()
        {
            List<CVRPlayerEntity> pl = GetUserlist();
            SetAllHeadSize(pl, 1f/Multipler.Value);
        }

        public static void NoHead()
        {
            List<CVRPlayerEntity> pl = GetUserlist();
            SetAllHeadSize(pl, 0f);
        }
    }
}