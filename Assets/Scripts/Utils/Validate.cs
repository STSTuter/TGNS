using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FYP.Utils
{
	public static class Validate
	{
		public static string ValidateText(string validCode, string textToValidate, string textOrigin)
		{

            if (textToValidate == null) return $"{textOrigin} field is empty";
            if (string.IsNullOrEmpty(textToValidate)) return $"{textOrigin} field is empty";
            if (textToValidate.Length < 6) return $"{textOrigin} is too short";
            return validCode;
		}
	}
}
