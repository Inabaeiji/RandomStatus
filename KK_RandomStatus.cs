using ActionGame;
using ActionGame.Chara;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Manager;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static SaveData;

namespace KK_RandomStatus
{
    public class ConfigurationManagerAttributes
    {
        public int? Order;
        public bool? Browsable;
        public string Category;
        public object DefaultValue;
        public bool? ReadOnly;
    }

    public static class AppInfo
    {
        public const string Guid = "com.inaba178.kk.randomstatus";
        public const string Name = "KK_RandomStatus";
        public const string Version = "1.0.0";
    }

    [BepInPlugin(AppInfo.Guid, AppInfo.Name, AppInfo.Version)]
    public class KK_RandomStatus : BaseUnityPlugin
    {
        private static bool isFirstMapMove = true;
        static ConfigEntry<bool> randomRelationFlag;
        static ConfigEntry<int> friend;
        static ConfigEntry<int> lover;
        static ConfigEntry<int> fullLover;
        static ConfigEntry<bool> randomLewdFlag;
        static ConfigEntry<int> lewdMax;
        static ConfigEntry<float> hDownMulti;
        static ConfigEntry<string> hDownPersonality;
        static ConfigEntry<bool> randomPersonalityFlag;
        static ConfigEntry<int> charaPersonality;
        static ConfigEntry<bool> allOkFlag;

        void Awake()
        {
            randomRelationFlag = Config.Bind("1.Randomize the relationship", "Enabled random relationship", true,
                new ConfigDescription("Enabled", null, new ConfigurationManagerAttributes { Order = 100 }));
            friend = Config.Bind("1.Randomize the relationship", "Friends", 4,
                new ConfigDescription("Percentage of friends", new AcceptableValueRange<int>(0, 10), new ConfigurationManagerAttributes { Order = 99 }));
            lover = Config.Bind("1.Randomize the relationship", "Lovers", 3,
                new ConfigDescription("Percentage of lovers", new AcceptableValueRange<int>(0, 10), new ConfigurationManagerAttributes { Order = 98 }));
            fullLover = Config.Bind("1.Randomize the relationship", "Full lovers", 1,
                new ConfigDescription("Percentage of full lovers", new AcceptableValueRange<int>(0, 10), new ConfigurationManagerAttributes { Order = 97 }));
            randomLewdFlag = Config.Bind("2.Randomize the H degree", "Enabled randome H degree", true,
                new ConfigDescription("Enabled", null, new ConfigurationManagerAttributes { Order = 100 }));
            lewdMax = Config.Bind("2.Randomize the H degree", "H degree that changes to horny", 90,
                new ConfigDescription("Change to horny if this value is greater then", new AcceptableValueRange<int>(51, 100), new ConfigurationManagerAttributes { Order = 99 }));
            hDownMulti = Config.Bind("2.Randomize the H degree", "Multiplier for the specified personality", 0.9f,
                new ConfigDescription("Multiply a random value by a specified value.", null, new ConfigurationManagerAttributes { Order = 98 }));
            hDownPersonality = Config.Bind("2.Randomize the H degree", "Personality ID to which the multiplier is applied", "0,7,24,33",
                new ConfigDescription("comma-separated(default:sexy, alluring, yandere, boyish)", null, new ConfigurationManagerAttributes { Order = 97 }));
            randomPersonalityFlag = Config.Bind("3.Randomize the personality", "Enabled random personality", true,
                new ConfigDescription("Enabled", null, new ConfigurationManagerAttributes { Order = 100 }));
            charaPersonality = Config.Bind("3.Randomize the personality", "Number of varieties", 39,
                new ConfigDescription("Number of varieties of personality", null, new ConfigurationManagerAttributes { Order = 99 }));
            allOkFlag = Config.Bind("4.Unlock all rejections, Maximize lesbian & masturbation desires", "Enabled Unlock  rejections & misc.", true,
                new ConfigDescription("Enabled", null, new ConfigurationManagerAttributes { Order = 100 }));

            var harmony = new Harmony(AppInfo.Guid);
            var method = typeof(Cycle).GetMethod("MapMove", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (method != null)
            {
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(KK_RandomStatus), nameof(GlobalPostfix)));
            }
            else
            {
                Debug.LogError("【RandomStatus】Not found: MapMove()");
            }
        }

        static void GlobalPostfix(Cycle __instance)
        {
            Debug.Log($"【RandomStatus】Start of time-based processing: {__instance.nowType}");

            //「活動中（LunchTime〜AfterSchool）」判定以外はスキップ
            if (!__instance.isAction)
            {
                return;
            }

            if (Game.Instance.HeroineList != null && Game.Instance.HeroineList.Count > 0)
            {
                AllGirlRandomizer(__instance);
            }
            else
            {
                Debug.Log("Processing was skipped because [Game.Instance.HeroineList] is not available.");
            }

            if (isFirstMapMove)
            {
                isFirstMapMove = false;
            }
        }

        static void AllGirlRandomizer(Cycle cycle)
        {
            var actionScene = cycle.GetComponent<ActionScene>();

            if (actionScene == null)
            {
                Debug.LogError("[RandomStatus] actionScene is null");
            }

            float weight = friend.Value + lover.Value + fullLover.Value;
            int[] specPersonality;
            int countF = 0;
            int countL = 0;
            int countFL = 0;
            int countH = 0;

            if (string.IsNullOrEmpty(hDownPersonality.Value) || string.IsNullOrEmpty(hDownPersonality.Value.Trim()))
            {
                specPersonality = new int[0];
            }
            else
            {
                specPersonality = hDownPersonality.Value.Split(',').Select(s => s.Trim()).Where(s => int.TryParse(s, out _)).Select(int.Parse).ToArray();
            }

            foreach (var h in Game.Instance.HeroineList)
            {
                if (h.fixCharaID < 0) continue;

                if (randomRelationFlag.Value)
                {
                    float randomRelationship = UnityEngine.Random.Range(0f, weight);

                    if (randomRelationship < friend.Value)
                    {
                        SetFriend(h);
                        countF++;
                    }
                    else if (randomRelationship < friend.Value + lover.Value)
                    {
                        SetLover(h);
                        countL++;
                    }
                    else
                    {
                        SetFullLover(h);
                        countFL++;
                    }
                }

                if (randomLewdFlag.Value)
                {
                    int randomLewd = UnityEngine.Random.Range(51, 101);

                    if (specPersonality.Length > 0 && specPersonality.Any(n => n == h.parameter.personality))
                    {
                        randomLewd = Mathf.Clamp((int)Math.Round(randomLewd * hDownMulti.Value), 0, 100);
                    }

                    if (randomLewd < lewdMax.Value)
                    {
                        h.lewdness = randomLewd;
                        MakeExperienced(h);
                    }
                    else
                    {
                        MakeHorny(h);
                        countH++;
                    }
                }
                
                if (randomPersonalityFlag.Value)
                {
                    int randomPersonality = UnityEngine.Random.Range(0, charaPersonality.Value);
                    h.parameter.personality = randomPersonality;

                    if (!isFirstMapMove)
                    {
                        NPC targetNpc = actionScene.npcList.Find(n => n.charaData == h);

                        if (targetNpc != null && targetNpc.initialized)
                        {
                            targetNpc.Replace(h);
                            targetNpc.AI.Reset(true);
                        }

                        var field = typeof(Heroine).GetField("cachedVoiceNo", BindingFlags.NonPublic | BindingFlags.Instance);

                        if (field != null)
                        {
                            field.SetValue(h, null);
                        }

                        h.ChaFileUpdate();
                    }
                }

                if (allOkFlag.Value)
                {
                    UnRejectionAll(h);
                    SetMasturbation(h);
                    SetLesbian(h);
                }

                h.talkEvent.Add(0); //初対面イベント済みにする。襲われ条件
                h.talkEvent.Add(1); //初対面イベント済みにする。襲われ条件
                h.talkEvent.Add(2); //友達イベント済みにする。なくても支障ない
            }

            Debug.Log($"Made {countF} friends");
            Debug.Log($"Made {countL} lovers");
            Debug.Log($"Made {countFL} full lovers");
            Debug.Log($"Made {countH} horny");
            Debug.Log("(Including characters on standby)");
        }

        static void UnRejectionAll(Heroine h)
        {
            h.denial.kiss = true;
            h.denial.massage = true;
            h.denial.anal = true;
            h.denial.aibu = true;
            h.denial.notCondom = true;
        }

        static void SetFriend(Heroine h)
        {
            h.anger = 0;
            h.isAnger = false;
            h.favor = 100;
            h.intimacy = 90;
            h.isGirlfriend = false;
            h.confessed = false;
        }

        static void SetLover(Heroine h)
        {
            h.anger = 0;
            h.isAnger = false;
            h.favor = 100;
            h.intimacy = 90;
            h.isGirlfriend = true;
            h.confessed = true;
        }

        static void SetFullLover(Heroine h)
        {
            h.anger = 0;
            h.isAnger = false;
            h.favor = 100;
            h.intimacy = 100;
            h.isGirlfriend = true;
            h.confessed = true;
        }

        static void SetMasturbation(Heroine h)
        {
            Game.Instance.actScene.actCtrl.SetDesire(4, h, 100);
        }

        static void SetLesbian(Heroine h)
        {
            Game.Instance.actScene.actCtrl.SetDesire(26, h, 100);
            Game.Instance.actScene.actCtrl.SetDesire(27, h, 100);
        }

        static void ClearDesires(Heroine h)
        {
            for (var i = 0; i < 31; i++)
                Game.Instance.actScene.actCtrl.SetDesire(i, h, 0);
        }

        private static void MakeHorny(Heroine h)
        {
            h.hCount = Mathf.Max(1, h.hCount);
            h.isVirgin = false;
            SetGirlHExp(h, 100f);
            h.lewdness = 100;
        }

        private static void MakeExperienced(Heroine h)
        {
            h.hCount = Mathf.Max(1, h.hCount);
            h.isVirgin = false;
            SetGirlHExp(h, 100f);
            h.lewdness = Mathf.Min(99, h.lewdness);
        }

        private static void MakeInexperienced(Heroine h)
        {
            h.hCount = Mathf.Max(1, h.hCount);
            h.isVirgin = false;
            h.countKokanH = 50;
            SetGirlHExp(h, 0);
        }

        private static void MakeVirgin(Heroine h)
        {
            h.hCount = 0;
            h.isVirgin = true;
            SetGirlHExp(h, 0);
        }

        private static void SetGirlHExp(Heroine girl, float amount)
        {
            girl.houshiExp = amount;
            girl.countKokanH = amount;
            girl.countAnalH = amount;
            for (var i = 0; i < girl.hAreaExps.Length; i++)
                girl.hAreaExps[i] = amount;
            for (var i = 0; i < girl.massageExps.Length; i++)
                girl.massageExps[i] = amount;
        }
    }
}
