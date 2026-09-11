using TMPro;
using UnityEngine;

namespace FYP.UI
{
    public class CoinDetails : MonoBehaviour
    {
        [SerializeField]
        Currency currency;
        [SerializeField]
        TMP_Text currencyText;

        private void Start()
        {
            UpdateGold();
        }
        private void OnEnable()
        {
            UpdateGold();
        }

        public void UpdateGold()
        {
            currencyText.text = currency.gold.ToString();
        }
    }
}
