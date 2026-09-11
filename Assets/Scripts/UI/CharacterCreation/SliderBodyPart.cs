using FYP.Character;
using UnityEngine;

namespace FYP.UI
{
    public class SliderBodyPart : SliderBase
    {
        [Header("Body Parts")]
        public BodyPartIndex bodyPart;
        [SerializeField]
        CharacterParts characterParts;

        public override void SliderValueChange()
        {
            base.SliderValueChange();
            characterParts.ChangeBodyPart(bodyPart, (int)sliderComp.value);
        }

    }
}