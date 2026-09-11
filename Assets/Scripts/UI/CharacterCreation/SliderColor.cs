using FYP.Character;
using System.Linq;
using UnityEngine;

namespace FYP.UI
{
    public class SliderColor : SliderBase
    {
        [Header("Color")]
        public BodyColorIndex colorIndex;
        [SerializeField]
        Color[] color;
        [SerializeField]
        CharacterParts characterParts;

        public override void SliderValueChange()
        {
            base.SliderValueChange();
            characterParts.SetColorOnMaterial(colorIndex, color[(int)sliderComp.value]);
        }

        public int GetColorCode(Color colorWanted)
        {
            for(int i=0;i<color.Length;i++)
            {
                if(colorWanted == color[i])
                    return i;
            }
            return 0;
        }
    }
}