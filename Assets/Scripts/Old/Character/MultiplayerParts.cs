using Unity.Netcode;
using UnityEngine;

namespace TGNS.Character
{
    /// <summary>
    /// Networked appearance sync for CharacterParts. No backend/persistence -
    /// the owner picks a look locally, pushes it to the server once, and the
    /// server replicates it to everyone (including late joiners) via a plain
    /// NetworkVariable, no external service/JSON round-trip involved.
    /// </summary>
    [RequireComponent(typeof(CharacterParts))]
    public class MultiplayerParts : NetworkBehaviour
    {
        private const int SlotCount = 10; // CharacterParts.BodyPartIndex entry count
        private const int ColorCount = 11; // CharacterParts.BodyColorIndex entry count

        private struct Appearance : INetworkSerializable, System.IEquatable<Appearance>
        {
            public FixedBytes10 Slots;
            public FixedColors11 Colors;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Slots);
                serializer.SerializeValue(ref Colors);
            }

            public bool Equals(Appearance other)
            {
                for (int i = 0; i < SlotCount; i++)
                {
                    if (Slots[i] != other.Slots[i]) return false;
                }

                for (int i = 0; i < ColorCount; i++)
                {
                    if (!Colors[i].Equals(other.Colors[i])) return false;
                }

                return true;
            }

            public override bool Equals(object obj) => obj is Appearance other && Equals(other);
            public override int GetHashCode() => Slots.b0 | (Slots.b1 << 8) | (Slots.b2 << 16) | (Slots.b3 << 24);
        }

        // Unity.Netcode ships FixedString/FixedBytes types for exactly this: small,
        // network-serializable fixed-size buffers without allocating per send.
        [System.Serializable]
        public struct FixedBytes10 : INetworkSerializable
        {
            public byte b0, b1, b2, b3, b4, b5, b6, b7, b8, b9;

            public byte this[int index]
            {
                get => index switch { 0 => b0, 1 => b1, 2 => b2, 3 => b3, 4 => b4, 5 => b5, 6 => b6, 7 => b7, 8 => b8, 9 => b9, _ => 0 };
                set
                {
                    switch (index)
                    {
                        case 0: b0 = value; break;
                        case 1: b1 = value; break;
                        case 2: b2 = value; break;
                        case 3: b3 = value; break;
                        case 4: b4 = value; break;
                        case 5: b5 = value; break;
                        case 6: b6 = value; break;
                        case 7: b7 = value; break;
                        case 8: b8 = value; break;
                        case 9: b9 = value; break;
                    }
                }
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref b0);
                serializer.SerializeValue(ref b1);
                serializer.SerializeValue(ref b2);
                serializer.SerializeValue(ref b3);
                serializer.SerializeValue(ref b4);
                serializer.SerializeValue(ref b5);
                serializer.SerializeValue(ref b6);
                serializer.SerializeValue(ref b7);
                serializer.SerializeValue(ref b8);
                serializer.SerializeValue(ref b9);
            }
        }

        [System.Serializable]
        public struct FixedColors11 : INetworkSerializable
        {
            public Color32 c0, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10;

            public Color32 this[int index]
            {
                get => index switch { 0 => c0, 1 => c1, 2 => c2, 3 => c3, 4 => c4, 5 => c5, 6 => c6, 7 => c7, 8 => c8, 9 => c9, 10 => c10, _ => default };
                set
                {
                    switch (index)
                    {
                        case 0: c0 = value; break;
                        case 1: c1 = value; break;
                        case 2: c2 = value; break;
                        case 3: c3 = value; break;
                        case 4: c4 = value; break;
                        case 5: c5 = value; break;
                        case 6: c6 = value; break;
                        case 7: c7 = value; break;
                        case 8: c8 = value; break;
                        case 9: c9 = value; break;
                        case 10: c10 = value; break;
                    }
                }
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref c0);
                serializer.SerializeValue(ref c1);
                serializer.SerializeValue(ref c2);
                serializer.SerializeValue(ref c3);
                serializer.SerializeValue(ref c4);
                serializer.SerializeValue(ref c5);
                serializer.SerializeValue(ref c6);
                serializer.SerializeValue(ref c7);
                serializer.SerializeValue(ref c8);
                serializer.SerializeValue(ref c9);
                serializer.SerializeValue(ref c10);
            }
        }

        private readonly NetworkVariable<Appearance> _appearance =
            new NetworkVariable<Appearance>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private CharacterParts _characterParts;

        private void Awake()
        {
            _characterParts = GetComponent<CharacterParts>();
        }

        public override void OnNetworkSpawn()
        {
            _appearance.OnValueChanged += HandleAppearanceChanged;

            if (IsOwner)
            {
                PushCurrentAppearance();
            }
            else
            {
                ApplyAppearance(_appearance.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            _appearance.OnValueChanged -= HandleAppearanceChanged;
        }

        /// <summary>Call after changing parts/colors locally on the owner to broadcast the new look.</summary>
        public void PushCurrentAppearance()
        {
            if (!IsOwner)
            {
                return;
            }

            Appearance appearance = default;
            for (int i = 0; i < SlotCount; i++)
            {
                appearance.Slots[i] = (byte)_characterParts.GetCurrentVariant((CharacterParts.BodyPartIndex)i);
            }

            for (int i = 0; i < ColorCount; i++)
            {
                appearance.Colors[i] = _characterParts.GetColor((CharacterParts.BodyColorIndex)i);
            }

            _appearance.Value = appearance;
        }

        private void HandleAppearanceChanged(Appearance previous, Appearance current)
        {
            if (IsOwner)
            {
                return; // the owner already applied its own change locally before pushing it
            }

            ApplyAppearance(current);
        }

        private void ApplyAppearance(Appearance appearance)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                _characterParts.ChangeBodyPart((CharacterParts.BodyPartIndex)i, appearance.Slots[i]);
            }

            for (int i = 0; i < ColorCount; i++)
            {
                _characterParts.SetColor((CharacterParts.BodyColorIndex)i, appearance.Colors[i]);
            }
        }
    }
}
