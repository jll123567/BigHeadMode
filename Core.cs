using MelonLoader;
using ABI_RC.Core.Player;
using UnityEngine;



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

        /// <summary>
        /// Set up melon prefs and icons for BTK.
        /// </summary>
        public override void OnInitializeMelon()
        {
            BigHeadModePrefrenceCategory = MelonPreferences.CreateCategory("BigHeadMode"); // Init melonprefs.
            ScaleLocal = MelonPreferences.CreateEntry<bool>("BigHeadMode", "ScaleLocal", true, "Scale Local Player", "Scale the local player head.");
            ScaleRemote = MelonPreferences.CreateEntry<bool>("BigHeadMode", "ScaleRemote", true, "Scale Remote Player", "Scale remote player's heads.");
            Multipler = MelonPreferences.CreateEntry<float>("BigHeadMode", "Multiplier", 2.0f, "Multiplier", "What to grow/shrink the head by (shrinks using `headScale * 1/multiplier`.");

            UI.LoadIcons(MelonAssembly.Assembly); // Init BTK buttons.
            UI.CreateCategory();  
            //LoggerInstance.Msg("BigHeadMode Initialized.");
        }

        /// <summary>
        /// Scale heads the user requested but were not loaded at the time.
        /// </summary>
        public override void OnUpdate()
        {
            base.OnUpdate();
            if (LateScaleQueue.Count > 0)  // Only process if we have to.
            {

                List<string> toClear = new List<string>();  // What entries we can remove.
                foreach (string uuid in LateScaleQueue.Keys)
                {
                    if (LateScaleQueue[uuid].Item1.transform != null && LateScaleQueue[uuid].Item1.transform.GetChild(2).childCount != 0)
                    {
                        //MelonLogger.Msg($"Late Scaling uuid {uuid}");
                        GameObject avatar = LateScaleQueue[uuid].Item1.transform.GetChild(2).GetChild(0).gameObject;  // Get and scale the bone now.
                        Animator animator = avatar.GetComponent<Animator>();
                        Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);
                        SetHeadSize(headBone, uuid, LateScaleQueue[uuid].Item2);
                        toClear.Add(uuid);  // Prepare to remove from LateScaleQueue.
                    }
                }
                foreach (string uuid in toClear) LateScaleQueue.Remove(uuid);  // Clear LateScaleQueue where possible.
            }
        }

        /// <summary>
        /// Get the list of users in the current instance as CVRPlayerEntities.
        /// </summary>
        /// <returns>CVRPlayerEntites of users in current instance.</returns>
        public static List<CVRPlayerEntity> GetUserlist()
        {
            List<CVRPlayerEntity> playerList = new List<CVRPlayerEntity>();
            if (!(ScaleLocal.Value || ScaleRemote.Value))  // If user requests nothing to happen, tell them that.
            {
                MelonLogger.Warning("ScaleLocal and ScaleRemote are both set to false. Set at least one to true if you want to grow/shrink any heads!");
            }
            if (ScaleRemote.Value)  // Get remote users if asked.
            {
                foreach (CVRPlayerEntity p in CVRPlayerManager.Instance.NetworkPlayers)
                {
                    playerList.Add(p);
                }
            }
            return playerList;
        }

        /// <summary>
        /// Scale all heads or queue heads to scale.
        /// </summary>
        /// <param name="players">List of players to scale.</param>
        /// <param name="multiplier">Value to multiply/divide head scale by.</param>
        public static void SetAllHeadSize(List<CVRPlayerEntity> players, float multiplier)
        {
            LateScaleQueue.Clear();  // New request from user, forget old and unfufilled requests.
            foreach (CVRPlayerEntity p in players)
            {
                try  // Because I'd rather fail quietly.
                {
                    //MelonLogger.Msg($"Scaling {p.Username} head by {multiplier}.");
                    if (p.PlayerObject.transform.GetChild(2).childCount != 0) {  // Dont try to scale if avatar is not loaded, queue it up for when it is.
                        GameObject avatar = p.PlayerObject.transform.GetChild(2).GetChild(0).gameObject;  // Get the head bone (in several steps).
                        Animator animator = avatar.GetComponent<Animator>();
                        Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);
                        SetHeadSize(headBone, p.Uuid, multiplier); // Scale the head bone.
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
            if (ScaleLocal.Value) // Scale the local user's head, if the want.
            {
                //MelonLogger.Msg($"Scaling local user head by {multiplier}.");
                Transform headBone = PlayerSetup.Instance.animatorManager.Animator.GetBoneTransform(HumanBodyBones.Head);
                SetHeadSize(headBone, "local", multiplier);  // Local user saves the origial scale with the key "local".
            }
        }
        /// <summary>
        /// Scale a transform and store the origial scale in OriginalHeadScales.
        /// </summary>
        /// <param name="headBone">HeadBone to scale.</param>
        /// <param name="uuid">UUID of the user who is being scaled.</param>
        /// <param name="multiplier">Value to scale by.</param>
        public static void SetHeadSize(Transform headBone, string uuid, float multiplier)
        {
            try  // I'd rather fail silently.
            {
                Vector3 originalScale;
                if (OriginalHeadScales.ContainsKey(uuid)) originalScale = OriginalHeadScales[uuid];  // Use the original scale if we have one stored.
                else
                {
                    originalScale = headBone.localScale;
                    OriginalHeadScales.Add(uuid, headBone.localScale);  // Save the original head scale so we can revert/rescale later...
                }
                headBone.localScale = new Vector3(originalScale.x * multiplier, originalScale.y * multiplier, originalScale.z * multiplier);
            }
            catch//(Exception e)
            {
                //MelonLogger.Error($"SetHeadSize Error:\n  {e}\n[{headBone} {uuid} {multiplier}]");
            }
        }

        /// <summary>
        /// Resore heads to original size using OriginalHeadScales.
        /// </summary>
        public static void RestoreHeadSize()
        {
            List<string> toClear = new List<string>();  // Heads that have successfully been restored.
            LateScaleQueue.Clear();  // If we had something we were going to scale, now we dont.
            foreach (string k in OriginalHeadScales.Keys)
            {
                if(k == "local")  // Handle restoring the local user.
                {
                    Transform headBone = PlayerSetup.Instance.animatorManager.Animator.GetBoneTransform(HumanBodyBones.Head);
                    headBone.localScale = OriginalHeadScales["local"];
                    toClear.Add("local");
                    continue;
                }
                
                try // Handle the remote user (since they aren't local).
                {
                    CVRPlayerEntity p = null;
                    foreach (CVRPlayerEntity pe in CVRPlayerManager.Instance.NetworkPlayers) // Search through the player list for our user, I don't trust CVRPlayerEntity to not explode on me later so I'm storing the UUID instead.
                    {
                        if (pe.Uuid == k && pe.PlayerObject != null) p = pe;
                    }
                    if (p == null) // Could not find the player to restore, they probably left...
                    {
                        toClear.Add(k);
                        continue;
                    }  
                    //MelonLogger.Msg($"Restoring {p.Username} head.");
                    GameObject avatar = p.PlayerObject.transform.GetChild(2).GetChild(0).gameObject; // Fix they head.
                    Animator animator = avatar.GetComponent<Animator>();
                    Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);
                    headBone.localScale = OriginalHeadScales[k];
                    toClear.Add(k);  // We fixed them!, we can remove them now...
                }
                catch //(Exception e)  // If something goes wrong dont mention it.
                {
                    //MelonLogger.Warning($"RestoreHeadSize failed:\n{e}");
                }
            }
            foreach (string k in toClear) OriginalHeadScales.Remove(k);  // Clear orignal scales since we're back to original.
        }

        /// <summary>
        /// Scale heads up, for BTK.
        /// </summary>
        public static void BigHead()
        {
            List<CVRPlayerEntity> pl = GetUserlist();
            SetAllHeadSize(pl, Multipler.Value);
        }

        /// <summary>
        /// Scale heads down, for BTK.
        /// </summary>
        public static void SmallHead()
        {
            List<CVRPlayerEntity> pl = GetUserlist();
            SetAllHeadSize(pl, 1f/Multipler.Value);
        }

        /// <summary>
        /// Scale heads to zero, for BTK.
        /// </summary>
        public static void NoHead()
        {
            List<CVRPlayerEntity> pl = GetUserlist();
            SetAllHeadSize(pl, 0f);
        }
    }
}