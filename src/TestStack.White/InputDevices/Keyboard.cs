using System.Collections.Generic;
using System.Linq;
using TestStack.White.UIItems.Actions;
using TestStack.White.WindowsAPI;

namespace TestStack.White.InputDevices
{
    // BUG: KeysConverter
    /// <summary>
    /// Represents Keyboard attachment to the machine.
    /// </summary>
    public class Keyboard : IKeyboard
    {
        private readonly List<KeyboardInput.SpecialKeys> heldKeys = new List<KeyboardInput.SpecialKeys>();
        protected readonly List<int> KeysHeld = new List<int>();
        
        public Keyboard()
        {
            heldKeys = new List<KeyboardInput.SpecialKeys>();
        }
        
        /// <summary>
        /// Implements <see cref="IKeyboard.Enter(string)"/>
        /// </summary>
        public virtual void Enter(string keysToType)
        {
            Send(keysToType, new NullActionListener());
        }

        /// <summary>
        /// Implements <see cref="IKeyboard.Send(string, IActionListener)"/>
        /// </summary>
        public virtual void Send(string keysToType, IActionListener actionListener)
        {
            if (HeldKeys.Count() > 0) keysToType = keysToType.ToLower();

            CapsLockOn = false;
            foreach (var key in from c in keysToType let key = BareMetalKeyboard.VkKeyScan(c) where !c.Equals('\r') select key)
            {
                if (BareMetalKeyboard.ShiftKeyIsNeeded(key))
                {
                    SendKeyDown((short)KeyboardInput.SpecialKeys.SHIFT, false);
                }
                if (BareMetalKeyboard.CtrlKeyIsNeeded(key))
                {
                    SendKeyDown((short)KeyboardInput.SpecialKeys.CONTROL, false);
                }
                if (BareMetalKeyboard.AltKeyIsNeeded(key))
                {
                    SendKeyDown((short)KeyboardInput.SpecialKeys.ALT, false);
                }
                Press(key, false);
                if (BareMetalKeyboard.ShiftKeyIsNeeded(key))
                {
                    SendKeyUp((short)KeyboardInput.SpecialKeys.SHIFT, false);
                }
                if (BareMetalKeyboard.CtrlKeyIsNeeded(key))
                {
                    SendKeyUp((short)KeyboardInput.SpecialKeys.CONTROL, false);
                }
                if (BareMetalKeyboard.AltKeyIsNeeded(key))
                {
                    SendKeyUp((short)KeyboardInput.SpecialKeys.ALT, false);
                }
            }

            actionListener.ActionPerformed(Action.WindowMessage);
        }

        /// <summary>
        /// Implements <see cref="IKeyboard.PressSpecialKey(KeyboardInput.SpecialKeys)"/>
        /// </summary>
        public virtual void PressSpecialKey(KeyboardInput.SpecialKeys key)
        {
            PressSpecialKey(key, new NullActionListener());
        }

        /// <summary>
        /// Implements <see cref="IKeyboard.PressSpecialKey(KeyboardInput.SpecialKeys, IActionListener)"/>
        /// </summary>
        public virtual void PressSpecialKey(KeyboardInput.SpecialKeys key, IActionListener actionListener)
        {
            Send(key, true);
            actionListener.ActionPerformed(Action.WindowMessage);
        }

        /// <summary>
        /// Implements <see cref="IKeyboard.HoldKey(KeyboardInput.SpecialKeys)"/>
        /// </summary>
        public virtual void HoldKey(KeyboardInput.SpecialKeys key)
        {
            HoldKey(key, new NullActionListener());
        }

        /// <summary>
        /// Implements <see cref="IKeyboard.HoldKey(KeyboardInput.SpecialKeys, IActionListener)"/>
        /// </summary>
        public virtual void HoldKey(KeyboardInput.SpecialKeys key, IActionListener actionListener)
        {
            SendKeyDown((short)key, true);
            AddUsedKey(key);
            actionListener.ActionPerformed(Action.WindowMessage);
        }

        /// <summary>
        /// Implements <see cref="IKeyboard.LeaveKey(KeyboardInput.SpecialKeys)"/>
        /// </summary>
        public virtual void LeaveKey(KeyboardInput.SpecialKeys key)
        {
            LeaveKey(key, new NullActionListener());
        }

        /// <summary>
        /// Implements <see cref="IKeyboard.LeaveKey(KeyboardInput.SpecialKeys, IActionListener)"/>
        /// </summary>
        public virtual void LeaveKey(KeyboardInput.SpecialKeys key, IActionListener actionListener)
        {
            SendKeyUp((short)key, true);
            RemoveUsedKey(key);
            actionListener.ActionPerformed(Action.WindowMessage);
        }

        /// <summary>
        /// Implements <see cref="IKeyboard.CapsLockOn"/>
        /// </summary>
        public virtual bool CapsLockOn
        {
            get
            {
                var state = BareMetalKeyboard.GetKeyState((uint)KeyboardInput.SpecialKeys.CAPS);
                return state != 0;
            }
            set
            {
                if (CapsLockOn != value)
                {
                    Send(KeyboardInput.SpecialKeys.CAPS, true);
                }
            }
        }

        /// <summary>
        /// Implements <see cref="IKeyboard.HeldKeys"/>
        /// </summary>
        public virtual KeyboardInput.SpecialKeys[] HeldKeys
        {
            get
            {
                return heldKeys.ToArray();
            }
        }

        /// <summary>
        /// Implements <see cref="IKeyboard.LeaveAllKeys()"/>
        /// </summary>
        public virtual void LeaveAllKeys()
        {
            new List<KeyboardInput.SpecialKeys>(heldKeys).ForEach(LeaveKey);
        }

        protected virtual void Press(short key, bool specialKey)
        {
            SendKeyDown(key, specialKey);
            SendKeyUp(key, specialKey);
        }

        protected virtual void Send(KeyboardInput.SpecialKeys key, bool specialKey)
        {
            Press((short)key, specialKey);
        }

        protected virtual void SendKeyUp(short b, bool specialKey)
        {
            if (!KeysHeld.Contains(b))
            {
                throw new InputDeviceException(string.Format("Cannot unpress the key {0}, it has not been pressed", b));
            }
            KeysHeld.Remove(b);
            var keyUpDown = BareMetalKeyboard.GetSpecialKeyCode(specialKey, KeyboardInput.KeyUpDown.KEYEVENTF_KEYUP);
            BareMetalKeyboard.SendInput(BareMetalKeyboard.GetInputFor(b, keyUpDown));
        }

        protected virtual void SendKeyDown(short b, bool specialKey)
        {
            if (KeysHeld.Contains(b))
            {
                throw new InputDeviceException(string.Format("Cannot press the key {0} as its already pressed", b));
            }
            KeysHeld.Add(b);
            var keyUpDown = BareMetalKeyboard.GetSpecialKeyCode(specialKey, KeyboardInput.KeyUpDown.KEYEVENTF_KEYDOWN);
            BareMetalKeyboard.SendInput(BareMetalKeyboard.GetInputFor(b, keyUpDown));
        }

        protected virtual void AddUsedKey(KeyboardInput.SpecialKeys key)
        {
            if (heldKeys.Contains(key))
            {
                return;
            }
            heldKeys.Add(key);
        }

        protected virtual void RemoveUsedKey(KeyboardInput.SpecialKeys key)
        {
            if (!heldKeys.Contains(key))
            {
                return;
            }
            heldKeys.Remove(key);
        }

        /// <summary>
        /// Implements <see cref="IKeyboard.ActionPerformed(IActionListener)"/>
        /// </summary>
        /// <remarks>
        public virtual void ActionPerformed(IActionListener actionListener)
        {
            actionListener.ActionPerformed(new Action(ActionType.WindowMessage));
        }
    }
}