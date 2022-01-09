using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyInputManager : MonoBehaviour
{
    public delegate void OnPlatformChangedDelegate(Platform newPlatform);

    public static MyInputManager Instance { get; private set; }
    public static Rewired.Player Local { get; private set; }
    public static Rewired.InputManager RewiredManager { get; private set; }
    public static Platform CurrentActivePlatform { get; private set; }
    public static event OnPlatformChangedDelegate OnPlatformChangedEvent;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Init();
    }

    private void Update()
    {
        foreach (KeyValuePair<string, BufferedAxis> axis in m_bufferedAxis)
        {
            axis.Value.Update(Local);
        }

        foreach (KeyValuePair<string, BufferedAxis2D> axis2D in m_2DBufferedAxis)
        {
            axis2D.Value.Update(Local);
        }
    }

    private void Init()
    {
        Local = Rewired.ReInput.players.GetPlayer(0);
        RewiredManager = GetComponent<Rewired.InputManager>();

        Local.controllers.AddLastActiveControllerChangedDelegate(OnLastActiveControllerChanged);

        UpdateActionJoystickMap();
        LoadControllerGUIDS();
    }

    private void OnLastActiveControllerChanged(Rewired.Player player, Rewired.Controller controller)
    {
        if(controller != null)
        {
            Debug.Log("Input platform changed: " + controller.hardwareName);
        }

        // Check if there was a change or a supplied platform
        Platform curPlatform = Instance.GetLastInputPlatform();

        if (curPlatform == CurrentActivePlatform)
        {
            return;
        }

        // Switch buttons for nintendo
        if (curPlatform == Platform.Nintendo && CurrentActivePlatform != Platform.Nintendo)
        {
            string old = InputAction.Inventory_Select;
            InputAction.Inventory_Select = InputAction.Inventory_Back;
            InputAction.Inventory_Back = old;
        }
        else if(CurrentActivePlatform == Platform.Nintendo && curPlatform != Platform.Nintendo)
        {
            string old = InputAction.Inventory_Select;
            InputAction.Inventory_Select = InputAction.Inventory_Back;
            InputAction.Inventory_Back = old;
        }

        CurrentActivePlatform = curPlatform;

        OnPlatformChanged();
    }

    private void OnPlatformChanged()
    {
        if (OnPlatformChangedEvent != null)
        {
            OnPlatformChangedEvent.Invoke(CurrentActivePlatform);
        }
    }

    public Platform GetLastInputPlatform()
    {
        Platform returnPlatform = GetDefaultPlatform();

        // See if the last controller is valid
        Rewired.Controller lastActive = Local.controllers.GetLastActiveController();
        if (lastActive == null)
        {
            return returnPlatform;
        }

        // If last was keyboard or mouse
        if (lastActive.identifier.controllerType == Rewired.ControllerType.Keyboard ||
            lastActive.identifier.controllerType == Rewired.ControllerType.Mouse ||
            lastActive.identifier.controllerType == Rewired.ControllerType.Custom)
        {
            return Platform.Windows;
        }

        // Try identify the controller
        if (m_controllerIdentifiers.TryGetValue(lastActive.hardwareTypeGuid, out Platform val))
        {
            returnPlatform = val;
        }

        // Return the device default
        return returnPlatform;
    }

    public Platform GetDefaultPlatform()
    {
        Platform returnPlatform;

#if UNITY_SWITCH
		returnPlatform = Platform.Switch;
#elif UNITY_PS4
		returnPlatform = Platform.PS4;
#elif UNITY_PS5
		returnPlatform = Platform.PS4;
#elif UNITY_STADIA
		returnPlatform = Platform.Stadia;
#elif UNITY_GAMECORE
		returnPlatform = Platform.XboxOne;
#else
        returnPlatform = Platform.Windows;
#endif

        return returnPlatform;
    }

    /// Checks the controllers hardware name and roughly matches the correct platform
    /// Doesn't discern between console generations i.e. Xbox360 and Xbox One will be the same 
    /// https://guavaman.com/projects/rewired/docs/SupportedControllers.html
    private void LoadControllerGUIDS()
    {
        TextAsset file = Resources.Load<TextAsset>("Data/RewiredControllers");
        if (!file)
        {
            Debug.LogError("Failed to find controller identifier file.");
            return;
        }
        m_controllerIdentifiers = new Dictionary<Guid, Platform>();
        string[] elements = file.text.Split('\n');
        for (int i = 2; i < elements.Length; ++i)
        {
            string[] values = elements[i].Split(',');
            if (values.Length != 3)
            {
                continue;
            }

            string deviceName = values[1];
            Guid curGUID = new Guid(values[2].Replace("\"", string.Empty));

            if (deviceName.IndexOf("xbox", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                m_controllerIdentifiers.Add(curGUID, Platform.Xbox);
            }
            else if (deviceName.IndexOf("sony", StringComparison.OrdinalIgnoreCase) >= 0 || deviceName.IndexOf("PS3", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                m_controllerIdentifiers.Add(curGUID, Platform.Playstation);
            }
            else if (deviceName.IndexOf("handheld", StringComparison.OrdinalIgnoreCase) >= 0 || deviceName.IndexOf("joy-con", StringComparison.OrdinalIgnoreCase) >= 0 || deviceName.IndexOf("nintendo", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                m_controllerIdentifiers.Add(curGUID, Platform.Nintendo);
            }
        }
    }

    public void UpdateActionJoystickMap()
    {
        m_actionToGlyphMappingCacheJoystick.Clear();

        AddGlyphsFromActionMapTable(RewiredManager.userData.GetJoystickMapById(0, out int mapidx).actionElementMaps, m_actionToGlyphMappingCacheJoystick, (map) => { return ((GamepadTemplateGlyphBinds)map.elementIdentifierId).ToString(); });
        AddGlyphsFromActionMapTable(RewiredManager.userData.GetKeyboardMapById(0, out int keyidx).actionElementMaps, m_actionToGlyphMappingCacheKeyboardMouse, (map) => { return map.keyCode.ToString(); });
        AddGlyphsFromActionMapTable(RewiredManager.userData.GetMouseMapById(0, out int mouseidx).actionElementMaps, m_actionToGlyphMappingCacheKeyboardMouse, (map) => 
        {
            Rewired.InputAction action = RewiredManager.userData.GetActionById(map.actionId);
            return $"Mouse{action.id}";
        });
    }

    private void AddGlyphsFromActionMapTable(List<Rewired.ActionElementMap> sourceMap, Dictionary<string, string> target, Func<Rewired.ActionElementMap, string> glyphBindFunc)
    {
        foreach (Rewired.ActionElementMap controllerActionMap in sourceMap)
        {
            string glyphBind = glyphBindFunc(controllerActionMap);

            Rewired.InputAction action = RewiredManager.userData.GetActionById(controllerActionMap.actionId);
            if(action == null)
            {
                continue;
            }

            string actionName = action.name;
            if (!target.ContainsKey(actionName))
            {
                string contribution = "";
                if (controllerActionMap.axisType != Rewired.AxisType.None)
                {
                    contribution = controllerActionMap.axisContribution == Rewired.Pole.Negative ? "-" : "+";
                }

                target.Add($"{actionName}{contribution}", glyphBind);
            }
            else
            {
                Debug.LogWarning($"Multiple glyphs bound to/duplicates of ({actionName}). Ignoring this extra binding.");
            }
        }
    }

    public bool TryGetGlyphBindingFromAction(string action, out string glyphBind)
    {
        bool onController = CurrentActivePlatform > Platform.Windows;
        glyphBind = "";
        if(onController)
        {
            if (m_actionToGlyphMappingCacheJoystick.TryGetValue(action, out string val))
            {
                glyphBind = val;
                return true;
            }
        }
        else
        {
            if (m_actionToGlyphMappingCacheKeyboardMouse.TryGetValue(action, out string val))
            {
                glyphBind = val;
                return true;
            }
        }

        return false;
    }

    public float GetBufferedAxis(string axis)
    {
        return GetBufferedAxis(axis, Local);
    }

    public float GetBufferedAxis(string axis, Rewired.Player player)
    {
        if (m_bufferedAxis.TryGetValue(axis, out BufferedAxis val))
        {
            return val.Value;
        }
        else
        {
            m_bufferedAxis.Add(axis, new BufferedAxis(axis));
            return player.GetAxis(axis);
        }
    }

    public Vector2 Get2DBufferedAxis(string xAxis, string yAxis, bool largestDeltaOnly = false)
    {
        return Get2DBufferedAxis(xAxis, yAxis, Local, largestDeltaOnly);
    }

    public Vector2 Get2DBufferedAxis(string xAxis, string yAxis, Rewired.Player player, bool largestDeltaOnly = false)
    {
        Vector2 input;
        string key = $"{xAxis}/{yAxis}";
        if (m_2DBufferedAxis.TryGetValue(key, out BufferedAxis2D val))
        {
            input = val.Value;
        }
        else
        {
            BufferedAxis2D axis2D = new BufferedAxis2D(xAxis, yAxis);
            m_2DBufferedAxis.Add(key, axis2D);
            input = axis2D.Value;
        }

        if (largestDeltaOnly)
        {
            if (Mathf.Abs(input.x) >= Mathf.Abs(input.y))
            {
                input.y = 0.0f;
            }
            else
            {
                input.x = 0.0f;
            }
        }

        return input;
    }


    private Dictionary<string, BufferedAxis> m_bufferedAxis = new Dictionary<string, BufferedAxis>();
    private Dictionary<string, BufferedAxis2D> m_2DBufferedAxis = new Dictionary<string, BufferedAxis2D>();
    private Dictionary<string, string> m_actionToGlyphMappingCacheJoystick = new Dictionary<string, string>();
    private Dictionary<string, string> m_actionToGlyphMappingCacheKeyboardMouse = new Dictionary<string, string>();
    private Dictionary<Guid, Platform> m_controllerIdentifiers;


    private class BufferedAxis
    {
        public BufferedAxis(string axisName, float deadzone = m_bufferDeadzone, float speed = m_bufferSpeed)
        {
            m_axisName = axisName;
            m_deadzoneThreshold = deadzone;
            m_speed = speed;
        }

        public float Value { get; private set; }


        public void Update(Rewired.Player input)
        {
            float axisValue = input.GetAxisRaw(m_axisName);

            if (Mathf.Abs(axisValue) <= m_deadzoneThreshold)
            {
                m_navTime = 0.0f;
            }

            if (m_navTime >= 0f)
            {
                m_navTime -= Time.deltaTime;
                Value = 0.0f;
                return;
            }

            if (axisValue > m_deadzoneThreshold || axisValue < -m_deadzoneThreshold)
            {
                Value = axisValue;
                m_navTime = m_speed;
            }
            else
            {
                Value = 0.0f;
            }
        }


        private float m_navTime = 0.0f;
        private readonly string m_axisName;
        private readonly float m_deadzoneThreshold;
        private readonly float m_speed;
    }
    private class BufferedAxis2D
    {
        public BufferedAxis2D(string xAxisName, string yAxisName, float deadzone = m_bufferDeadzone, float speed = m_bufferSpeed)
        {
            m_xAxisName = xAxisName;
            m_yAxisName = yAxisName;
            m_deadzoneThreshold = deadzone;
            m_speed = speed;
        }

        public Vector2 Value { get; private set; }


        public void Update(Rewired.Player input)
        {
            float xAxisVal = input.GetAxisRaw(m_xAxisName);
            float yAxisVal = input.GetAxisRaw(m_yAxisName);

            if (Mathf.Abs(xAxisVal) <= m_deadzoneThreshold && Mathf.Abs(yAxisVal) <= m_deadzoneThreshold)
            {
                m_navTime = 0.0f;
            }

            if (m_navTime >= 0f)
            {
                m_navTime -= Time.deltaTime;
                Value = Vector2.zero;
                return;
            }

            float x = TestAxis(xAxisVal);
            float y = TestAxis(yAxisVal);
            if (x != 0.0f || y != 0.0f)
            {
                m_navTime = m_speed;
            }

            Value = new Vector2(x, y);
        }

        private float TestAxis(float rawInput)
        {
            float val = 0.0f;
            if (rawInput > m_deadzoneThreshold || rawInput < -m_deadzoneThreshold)
            {
                val = rawInput;
            }
            return val;
        }


        private float m_navTime = 0.0f;
        private readonly string m_xAxisName;
        private readonly string m_yAxisName;
        private readonly float m_deadzoneThreshold;
        private readonly float m_speed;
    }

    private enum GamepadTemplateGlyphBinds
    {
        LeftStickX = 0,
        LeftStickY = 1,
        RightStickX = 2,
        RightStickY = 3,
        ActionBottomRow1 = 4, // A
        ActionBottomRow2 = 5, // B
        ActionBottomRow3 = 6,
        ActionTopRow1 = 7, // X
        ActionTopRow2 = 8, // Y
        ActionTopRow3 = 9,
        LeftShoulder1 = 10, // LB
        LeftShoulder2 = 11, // LT
        RightShoulder1 = 12, // RB
        RightShoulder2 = 13, // RT
        Center1 = 14, // Select
        Center2 = 15, // Start
        Center3 = 16, // Xbox Button
        LeftStickButton = 17,
        RightStickButton = 18,
        DPadUp = 19,
        DPadRight = 20,
        DPadDown = 21,
        DPadLeft = 22,
        LeftStick = 23,
        RightStick = 24,
        DPad = 25,
    }
    private enum MouseGlyphBinds
    {

    }
    public enum Platform
    {
        Unknown = -1,
        Windows = 0,
        Playstation,
        Xbox,
        Nintendo
    }



    private const float m_bufferDeadzone = 0.2f;
    private const float m_bufferSpeed = 0.15f;
}
