using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nullcore.InputSystem
{
    public static class KeybindManager
    {
        public const string Left = "Left";
        public const string Right = "Right";
        public const string Jump = "Jump";
        public const string ToggleRunWalk = "ToggleRunWalk";
        public const string Sprint = "Sprint";
        public const string Dash = "Dash";
        public const string Use = "Use";

        private static readonly Dictionary<string, Key[]> Defaults = new Dictionary<string, Key[]>
        {
            { Left, new[] { Key.A, Key.None } },
            { Right, new[] { Key.D, Key.None } },
            { Jump, new[] { Key.Space, Key.W } },
            { ToggleRunWalk, new[] { Key.CapsLock, Key.None } },
            { Sprint, new[] { Key.LeftShift, Key.None } },
            { Dash, new[] { Key.LeftCtrl, Key.None } },
            { Use, new[] { Key.E, Key.None } },
        };

        private static Dictionary<string, Key[]> current;

        private static Dictionary<string, Key[]> Current
        {
            get
            {
                if (current == null)
                {
                    Load();
                }
                return current;
            }
        }

        public static Key GetKey(string action, int slot)
        {
            return Current[action][slot];
        }

        public static void SetKey(string action, int slot, Key key)
        {
            Current[action][slot] = key;
        }

        public static bool IsPressed(Keyboard keyboard, string action)
        {
            var keys = Current[action];
            return IsKeyPressed(keyboard, keys[0]) || IsKeyPressed(keyboard, keys[1]);
        }

        public static bool WasPressedThisFrame(Keyboard keyboard, string action)
        {
            var keys = Current[action];
            return WasKeyPressedThisFrame(keyboard, keys[0]) || WasKeyPressedThisFrame(keyboard, keys[1]);
        }

        private static bool IsKeyPressed(Keyboard keyboard, Key key)
        {
            return key != Key.None && keyboard[key].isPressed;
        }

        private static bool WasKeyPressedThisFrame(Keyboard keyboard, Key key)
        {
            return key != Key.None && keyboard[key].wasPressedThisFrame;
        }

        public static string GetDisplayName(string action, int slot)
        {
            return GetDisplayName(Current[action][slot]);
        }

        public static string GetDisplayName(Key key)
        {
            switch (key)
            {
                case Key.None:
                    return string.Empty;
                case Key.LeftShift:
                case Key.RightShift:
                    return "Shift";
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    return "Ctrl";
                case Key.CapsLock:
                    return "Caps";
                default:
                    return key.ToString();
            }
        }

        public static void Load()
        {
            current = new Dictionary<string, Key[]>();
            foreach (var kvp in Defaults)
            {
                var keys = new Key[2];
                for (int slot = 0; slot < 2; slot++)
                {
                    string prefKey = PrefKey(kvp.Key, slot);
                    keys[slot] = PlayerPrefs.HasKey(prefKey)
                        ? (Key)PlayerPrefs.GetInt(prefKey)
                        : kvp.Value[slot];
                }
                current[kvp.Key] = keys;
            }
        }

        public static void Save()
        {
            foreach (var kvp in Current)
            {
                for (int slot = 0; slot < 2; slot++)
                {
                    PlayerPrefs.SetInt(PrefKey(kvp.Key, slot), (int)kvp.Value[slot]);
                }
            }
            PlayerPrefs.Save();
        }

        public static void Revert()
        {
            Load();
        }

        private static string PrefKey(string action, int slot)
        {
            return "Keybind_" + action + "_" + slot;
        }
    }
}
