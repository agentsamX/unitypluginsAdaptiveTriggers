using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AOT;
using UnityEngine;

namespace Apple.GameController.Controller
{
    #region Delegate Type Definitions
    public delegate void ControllerConnectionStateChangedCallback(GCControllerHandle handle);
    #endregion

    public static partial class GCControllerService
    {
        private static Dictionary<string, GCController> _controllers = new Dictionary<string, GCController>();

        public static event EventHandler<ControllerConnectedEventArgs> ControllerConnected;
        public static event EventHandler<ControllerConnectedEventArgs> ControllerDisconnected;

        #region Initialize
        public static void Initialize()
        {
            SetConnectionHandlers(OnControllerConnected, OnControllerDisconnected);
        }

        [MonoPInvokeCallback(typeof(ControllerConnectionStateChangedCallback))]
        private static void OnControllerConnected(GCControllerHandle handle)
        {
            TryConnectController(handle);
        }

        private static void TryConnectController(GCControllerHandle handle)
        {
            // Add controller if not already tracked...
            if (!_controllers.ContainsKey(handle.UniqueId))
            {
                _controllers[handle.UniqueId] = new GCController(handle);

                // Update the handle with latest info...
                _controllers[handle.UniqueId].Handle = handle;
                _controllers[handle.UniqueId].NotifiyConnectedStateChanged(new ConnectionStateChangeEventArgs(true));

                // Notify connected...
                ControllerConnected?.Invoke(null, new ControllerConnectedEventArgs(_controllers[handle.UniqueId]));
            }
        }

        [MonoPInvokeCallback(typeof(ControllerConnectionStateChangedCallback))]
        private static void OnControllerDisconnected(GCControllerHandle handle)
        {
            TryDisconnectController(handle);
        }

        private static void TryDisconnectController(GCControllerHandle handle)
        {
            if (_controllers.ContainsKey(handle.UniqueId))
            {
                _controllers[handle.UniqueId].Handle = handle;
                _controllers[handle.UniqueId].NotifiyConnectedStateChanged(new ConnectionStateChangeEventArgs(false));

                // Notify disconnected...
                ControllerDisconnected?.Invoke(null, new ControllerConnectedEventArgs(_controllers[handle.UniqueId]));

                // Remove the tracked controller...
                _controllers.Remove(handle.UniqueId);
            }
        }
        #endregion

        #region Poll All Controllers
        public static void PollAllControllers()
        {
            foreach(var pair in _controllers)
            {
                if(pair.Value.IsConnected)
                {
                    pair.Value.Poll();
                }
            }
        }
        #endregion

        #region Get Connected Controllers
        public static IEnumerable<GCController> GetConnectedControllers()
        {
            // Ensure existing controllers are "connected"...
            foreach(var handle in GetConnectedControllerHandles())
            {
                TryConnectController(handle);
            }

            return _controllers.Values;
        }
        #endregion

        #region Start Wireless Discovery
        [DllImport(InteropUtility.DLLName)]
        private static extern void GameControllerWrapper_StartWirelessDiscovery(SuccessCallback onComplete);

        private static TaskCompletionSource<bool> _startWirelessDiscoveryTCS;

        public static Task StartWirelessDiscovery()
        {
            if (_startWirelessDiscoveryTCS != null)
                throw new InvalidOperationException("An operation is already in progress.");

            _startWirelessDiscoveryTCS = new TaskCompletionSource<bool>();

            GameControllerWrapper_StartWirelessDiscovery(OnWirelessDiscoveryComplete);

            return _startWirelessDiscoveryTCS.Task;
        }

        [MonoPInvokeCallback(typeof(SuccessCallback))]
        private static void OnWirelessDiscoveryComplete()
        {
            _startWirelessDiscoveryTCS?.SetResult(true);
            _startWirelessDiscoveryTCS = null;
        }
        #endregion

        #region Stop Wireless Discovery
        [DllImport(InteropUtility.DLLName)]
        private static extern void GameControllerWrapper_StopWirelessDiscovery();

        public static void StopWirelessDiscovery()
        {
            GameControllerWrapper_StopWirelessDiscovery();
        }
        #endregion

        #region Set Connection Handlers
        [DllImport(InteropUtility.DLLName)]
        private static extern void GameControllerWrapper_SetConnectionHandlers(ControllerConnectionStateChangedCallback onConnected, ControllerConnectionStateChangedCallback onDisconnected);

        private static void SetConnectionHandlers(ControllerConnectionStateChangedCallback onConnected, ControllerConnectionStateChangedCallback onDisconnected)
        {
            GameControllerWrapper_SetConnectionHandlers(onConnected, onDisconnected);
        }
        #endregion

        #region Get Connected Controller Handles
        [DllImport(InteropUtility.DLLName)]
        private static extern GCGetConnectedControllersResponse GameControllerWrapper_GetConnectedControllers();

        public static GCControllerHandle[] GetConnectedControllerHandles()
        {
            // TODO: Instead of returning the handle, return a GCController based on the unique id...
            var response = GameControllerWrapper_GetConnectedControllers();
            return response.GetControllers();
        }
        #endregion

        #region Poll Controller
        [DllImport(InteropUtility.DLLName)]
        private static extern GCControllerInputState GameControllerWrapper_PollController(string uniqueId);

        public static GCControllerInputState PollController(GCControllerHandle controllerHandle)
        {
            return GameControllerWrapper_PollController(controllerHandle.UniqueId);
        }
        #endregion

        #region Controller Light Color
        [DllImport(InteropUtility.DLLName)]
        private static extern void GameControllerWrapper_SetControllerLightColor(string uniqueId, float red, float green, float blue);

        public static void SetControllerLightColor(GCControllerHandle controllerHandle, float red, float green, float blue)
        {
            GameControllerWrapper_SetControllerLightColor(controllerHandle.UniqueId, red, green, blue);
            Debug.Log($"Setting controller {controllerHandle.UniqueId} to [{red}, {green}, {blue}]");
        }
        #endregion
        
        #region Controller Adaptive Triggers

        [DllImport(InteropUtility.DLLName)]
        private static extern void GameControllerWrapper_SetControllerAdaptiveLeftFeedback(string uniqueId, float startPosition, float resistiveStrength);

        public static void SetControllerAdaptiveLeftFeedback(GCControllerHandle controllerHandle, float startPosition, float resistiveStrength)
        {
            GameControllerWrapper_SetControllerAdaptiveLeftFeedback(controllerHandle.UniqueId, startPosition, resistiveStrength);
            Debug.Log($"Setting controller {controllerHandle.UniqueId} left trigger to Feedback [{startPosition}, {resistiveStrength}]");
        }

        [DllImport(InteropUtility.DLLName)]
        private static extern void GameControllerWrapper_SetControllerAdaptiveRightFeedback(string uniqueId, float startPosition, float resistiveStrength);

        public static void SetControllerAdaptiveRightFeedback(GCControllerHandle controllerHandle, float startPosition, float resistiveStrength)
        {
            GameControllerWrapper_SetControllerAdaptiveRightFeedback(controllerHandle.UniqueId, startPosition, resistiveStrength);
            Debug.Log($"Setting controller {controllerHandle.UniqueId} right trigger to Feedback [{startPosition}, {resistiveStrength}]");
        }

        [DllImport(InteropUtility.DLLName)]
        private static extern void GameControllerWrapper_SetControllerAdaptiveLeftWeapon(string uniqueId, float startPosition, float endPosition, float resistiveStrength);

        public static void SetControllerAdaptiveLeftWeapon(GCControllerHandle controllerHandle, float startPosition, float endPosition, float resistiveStrength)
        {
            GameControllerWrapper_SetControllerAdaptiveLeftWeapon(controllerHandle.UniqueId, startPosition, endPosition, resistiveStrength);
            Debug.Log($"Setting controller {controllerHandle.UniqueId} left trigger to Weapon [{startPosition}, {endPosition} {resistiveStrength}]");
        }

        [DllImport(InteropUtility.DLLName)]
        private static extern void GameControllerWrapper_SetControllerAdaptiveRightWeapon(string uniqueId, float startPosition, float endPosition, float resistiveStrength);

        public static void SetControllerAdaptiveRightWeapon(GCControllerHandle controllerHandle, float startPosition, float endPosition, float resistiveStrength)
        {
            GameControllerWrapper_SetControllerAdaptiveRightWeapon(controllerHandle.UniqueId, startPosition, endPosition, resistiveStrength);
            Debug.Log($"Setting controller {controllerHandle.UniqueId} right trigger to Weapon [{startPosition}, {endPosition} {resistiveStrength}]");
        }

        [DllImport(InteropUtility.DLLName)]
        private static extern void GameControllerWrapper_SetControllerAdaptiveLeftSlope(string uniqueId, float startPosition, float endPosition, float startStrength, float endStrength);

        public static void SetControllerAdaptiveLeftSlope(GCControllerHandle controllerHandle, float startPosition, float endPosition, float startStrength, float endStrength)
        {
            GameControllerWrapper_SetControllerAdaptiveLeftSlope(controllerHandle.UniqueId, startPosition, endPosition, startStrength, endStrength);
            Debug.Log($"Setting controller {controllerHandle.UniqueId} left trigger to Slope [{startPosition}, {endPosition} {startStrength}, {endStrength}]");
        }

        [DllImport(InteropUtility.DLLName)]
        private static extern void GameControllerWrapper_SetControllerAdaptiveRightSlope(string uniqueId, float startPosition, float endPosition, float startStrength, float endStrength);

        public static void SetControllerAdaptiveRightSlope(GCControllerHandle controllerHandle, float startPosition, float endPosition, float startStrength, float endStrength)
        {
            GameControllerWrapper_SetControllerAdaptiveRightSlope(controllerHandle.UniqueId, startPosition, endPosition, startStrength, endStrength);
            Debug.Log($"Setting controller {controllerHandle.UniqueId} right trigger to Slope [{startPosition}, {endPosition} {startStrength}, {endStrength}]");
        }

        [DllImport(InteropUtility.DLLName)]
        private static extern void GameControllerWrapper_SetControllerAdaptiveLeftVibration(string uniqueId, float startPosition, float amplitude, float frequency);

        public static void SetControllerAdaptiveLeftVibration(GCControllerHandle controllerHandle, float startPosition, float amplitude, float frequency)
        {
            GameControllerWrapper_SetControllerAdaptiveLeftVibration(controllerHandle.UniqueId, startPosition, amplitude, frequency);
            Debug.Log($"Setting controller {controllerHandle.UniqueId} left trigger to Vibration [{startPosition}, {amplitude} {frequency}]");
        }

        [DllImport(InteropUtility.DLLName)]
        private static extern void GameControllerWrapper_SetControllerAdaptiveRightVibration(string uniqueId, float startPosition, float amplitude, float frequency);

        public static void SetControllerAdaptiveRightVibration(GCControllerHandle controllerHandle, float startPosition, float amplitude, float frequency)
        {
            GameControllerWrapper_SetControllerAdaptiveRightVibration(controllerHandle.UniqueId, startPosition, amplitude, frequency);
            Debug.Log($"Setting controller {controllerHandle.UniqueId} right trigger to Vibration [{startPosition}, {amplitude} {frequency}]");
        }

        [DllImport(InteropUtility.DLLName)]
        private static extern void GameControllerWrapper_SetControllerAdaptiveLeftPositionalVibration(string uniqueId, float frequency, float amplitude1, float amplitude2, float amplitude3, float amplitude4, float amplitude5, float amplitude6, float amplitude7, float amplitude8, float amplitude9, float amplitude10);

        public static void SetControllerAdaptiveLeftPositionalVibration(GCControllerHandle controllerHandle, float frequency, float amplitude1, float amplitude2, float amplitude3, float amplitude4, float amplitude5, float amplitude6, float amplitude7, float amplitude8, float amplitude9, float amplitude10)
        {
            GameControllerWrapper_SetControllerAdaptiveLeftPositionalVibration(controllerHandle.UniqueId, frequency, amplitude1, amplitude2, amplitude3, amplitude4, amplitude5, amplitude6, amplitude7, amplitude8, amplitude9, amplitude10);
            Debug.Log($"Setting controller {controllerHandle.UniqueId} left trigger to Positional Vibration [{frequency}]");
        }

        [DllImport(InteropUtility.DLLName)]
        private static extern void GameControllerWrapper_SetControllerAdaptiveRightPositionalVibration(string uniqueId, float frequency, float amplitude1, float amplitude2, float amplitude3, float amplitude4, float amplitude5, float amplitude6, float amplitude7, float amplitude8, float amplitude9, float amplitude10);

        public static void SetControllerAdaptiveRightPositionalVibration(GCControllerHandle controllerHandle, float frequency, float amplitude1, float amplitude2, float amplitude3, float amplitude4, float amplitude5, float amplitude6, float amplitude7, float amplitude8, float amplitude9, float amplitude10)
        {
            GameControllerWrapper_SetControllerAdaptiveRightPositionalVibration(controllerHandle.UniqueId, frequency, amplitude1, amplitude2, amplitude3, amplitude4, amplitude5, amplitude6, amplitude7, amplitude8, amplitude9, amplitude10);
            Debug.Log($"Setting controller {controllerHandle.UniqueId} right trigger to Positional Vibration [{frequency}]");
        }

        [DllImport(InteropUtility.DLLName)]
        private static extern void GameControllerWrapper_SetControllerAdaptiveLeftPositionalResistance(string uniqueId, float strength1, float strength2, float strength3, float strength4, float strength5, float strength6, float strength7, float strength8, float strength9, float strength10);

        public static void SetControllerAdaptiveLeftPositionalResistance(GCControllerHandle controllerHandle, float strength1, float strength2, float strength3, float strength4, float strength5, float strength6, float strength7, float strength8, float strength9, float strength10)
        {
            GameControllerWrapper_SetControllerAdaptiveLeftPositionalResistance(controllerHandle.UniqueId, strength1, strength2, strength3, strength4, strength5, strength6, strength7, strength8, strength9, strength10);
            Debug.Log($"Setting controller {controllerHandle.UniqueId} left trigger to Positional Resistance ");
        }

        [DllImport(InteropUtility.DLLName)]
        private static extern void GameControllerWrapper_SetControllerAdaptiveRightPositionalResistance(string uniqueId, float strength1, float strength2, float strength3, float strength4, float strength5, float strength6, float strength7, float strength8, float strength9, float strength10);

        public static void SetControllerAdaptiveRightPositionalResistance(GCControllerHandle controllerHandle, float strength1, float strength2, float strength3, float strength4, float strength5, float strength6, float strength7, float strength8, float strength9, float strength10)
        {
            GameControllerWrapper_SetControllerAdaptiveRightPositionalResistance(controllerHandle.UniqueId, strength1, strength2, strength3, strength4, strength5, strength6, strength7, strength8, strength9, strength10);
            Debug.Log($"Setting controller {controllerHandle.UniqueId} right trigger to Positional Resistance ");
        }
        
        #endregion
        
        #region Get Symbol for Input Name
        [DllImport(InteropUtility.DLLName)]
        private static extern GCGetSymbolForInputNameResponse GameControllerWrapper_GetSymbolForInputName(string uniqueId, GCControllerInputName inputName, GCControllerSymbolScale symbolScale, GCControllerRenderingMode renderingMode);

        public static Texture2D GetSymbolForInputName(GCControllerHandle controllerHandle, GCControllerInputName inputName, GCControllerSymbolScale symbolScale, GCControllerRenderingMode renderingMode)
        {
            var response = GameControllerWrapper_GetSymbolForInputName(controllerHandle.UniqueId, inputName, symbolScale, renderingMode);
            return response.GetTexture();
        }
        #endregion
    }
}