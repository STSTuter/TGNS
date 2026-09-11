using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Menu
{
    public class MenuWebsite : MonoBehaviour
    {
        [SerializeField]
        const string WEBSITE_URL = "https://www.petrestefan.com";
        public void OpenWebsite()
        {
            Application.OpenURL(WEBSITE_URL);
        }
    }
}