using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using TMPro;

using Newtonsoft.Json;

using FYP.Character;
using FYP.Steam;
using FYP.Utils;

namespace FYP.PlayFabIntegration
{
    public class BodyData
    {
        public string characterName { get; set; }
        public Dictionary<BodyPartIndex, int> bodyParts { get; set; } = new Dictionary<BodyPartIndex, int>();
        [JsonConverter(typeof(BodyColorIndexColorDictionaryConverter))]
        public Dictionary<BodyColorIndex, Color> bodyColors { get; set; } = new Dictionary<BodyColorIndex, Color>();
    }

    /// <summary>
    /// Local (no backend) character-appearance persistence and gold wallet. Each Steam account on
    /// this machine gets its own save file under Application.persistentDataPath, so two Steam
    /// accounts sharing a PC don't clobber each other's character. Historically this round-tripped
    /// through PlayFab's UserData/Economy services; that dependency is gone, the JSON shape and the
    /// SetData()/SetDataToCharacter() contract used for network replication (see MultiplayerParts)
    /// are unchanged.
    /// </summary>
    public class PlayfabCharacter : MonoBehaviour
    {
        private const int StartingGold = 100;

        public bool getDataOnStart;
        public CharacterParts character;
        public UI.Currency currency;
        [SerializeField]
        TMP_Text outputText;

        public string playerId;

        public BodyData cachedBodyData;

        [Serializable]
        private class LocalSaveData
        {
            public string bodyDataJson;
            public int gold = StartingGold;
        }

        private LocalSaveData _save;

        private void Start()
        {
            playerId = ResolveLocalPlayerId();
            if (getDataOnStart) GetData();
        }

        private static string ResolveLocalPlayerId()
        {
            if (SteamBootstrap.Instance != null && SteamBootstrap.Instance.Initialized)
            {
                return SteamBootstrap.Instance.LocalSteamId.m_SteamID.ToString();
            }

            // Steam not running (e.g. running the Editor without Steam) - fall back to a stable
            // per-machine id so local saves still work while testing.
            return "local_" + SystemInfo.deviceUniqueIdentifier;
        }

        private string SavePath => Path.Combine(Application.persistentDataPath, $"character_{playerId}.json");

        public void GetData()
        {
            if (string.IsNullOrEmpty(playerId))
            {
                playerId = ResolveLocalPlayerId();
            }

            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    _save = JsonConvert.DeserializeObject<LocalSaveData>(json) ?? new LocalSaveData();
                }
                else
                {
                    _save = new LocalSaveData();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayfabCharacter] Failed to read local save: {e.Message}");
                _save = new LocalSaveData();
            }

            if (currency != null)
            {
                currency.gold = _save.gold;
            }

            if (!string.IsNullOrEmpty(_save.bodyDataJson))
            {
                SetDataToCharacter(_save.bodyDataJson);
            }
        }

        public void SetDataToCharacter(string resultString)
        {
            BodyData bodyData = JsonConvert.DeserializeObject<BodyData>(resultString);
            cachedBodyData = bodyData;
            character.characterName = bodyData.characterName;

            var bodyParts = bodyData.bodyParts;
            foreach (var bp in bodyParts)
            {
                character.ChangeBodyPart(bp.Key, bp.Value);
            }

            var bodyColors = bodyData.bodyColors;
            character.CreateMaterial(
                bodyColors[BodyColorIndex._Color_Skin],
                bodyColors[BodyColorIndex._Color_Hair],
                bodyColors[BodyColorIndex._Color_Eyes],
                bodyColors[BodyColorIndex._Color_BodyArt],
                bodyColors[BodyColorIndex._Color_Primary],
                bodyColors[BodyColorIndex._Color_Secondary],
                bodyColors[BodyColorIndex._Color_Leather_Primary],
                bodyColors[BodyColorIndex._Color_Leather_Secondary],
                bodyColors[BodyColorIndex._Color_Metal_Primary],
                bodyColors[BodyColorIndex._Color_Metal_Secondary],
                bodyColors[BodyColorIndex._Color_Metal_Dark]);
        }

        public string SetData()
        {
            BodyData bodyData = new BodyData();
            bodyData.characterName = character.characterName;
            bodyData.bodyParts = SetBodyParts();
            bodyData.bodyColors = SetColors();

            string serializeString = JsonConvert.SerializeObject(bodyData);

            _save ??= new LocalSaveData { gold = StartingGold };
            _save.bodyDataJson = serializeString;
            WriteLocalSave();

            if (outputText != null)
            {
                outputText.text = "Character created successfully !";
            }

            return serializeString;
        }

        private Dictionary<BodyPartIndex, int> SetBodyParts()
        {
            Dictionary<BodyPartIndex, int> bodyParts = new Dictionary<BodyPartIndex, int>();
            foreach (var bp in character.bodyParts)
            {
                bodyParts[bp.bodyPartEnum] = bp.currentBodyPartId;
            }
            return bodyParts;
        }

        public Dictionary<BodyColorIndex, Color> SetColors()
        {
            Dictionary<BodyColorIndex, Color> bodyColors = new Dictionary<BodyColorIndex, Color>();
            foreach (BodyColorIndex colorIndex in Enum.GetValues(typeof(BodyColorIndex)))
            {
                bodyColors[colorIndex] = character.currentMaterial.GetColor(Enum.GetName(typeof(BodyColorIndex), colorIndex));
            }
            return bodyColors;
        }

        public void GetGold()
        {
            if (currency != null && _save != null)
            {
                currency.gold = _save.gold;
            }
        }

        /// <summary>Spend gold locally. Returns false (and leaves the wallet untouched) if the wallet can't cover it.</summary>
        public bool TrySpendGold(int amount)
        {
            _save ??= new LocalSaveData { gold = StartingGold };
            if (_save.gold < amount)
            {
                return false;
            }

            _save.gold -= amount;
            WriteLocalSave();
            GetGold();
            return true;
        }

        private void WriteLocalSave()
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(SavePath, JsonConvert.SerializeObject(_save));
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayfabCharacter] Failed to write local save: {e.Message}");
                if (outputText != null)
                {
                    outputText.text = "Could not save character locally.";
                }
            }
        }
    }
}
