using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ruccho.Utilities.Editors
{
    [CustomPropertyDrawer(typeof(TypeRestrictedExposedReferenceAttribute))]
    public class TypeRestrictedExposedReferenceDrawer : BaseExposedPropertyDrawer
    {
        public Type Restriction { get; }
        public TypeRestrictedExposedReferenceDrawer(Type restriction) : base()
        {
            Restriction = restriction;
        }
        
        private static bool CheckForCrossSceneReferencing(Object obj1, Object obj2)
        {
            GameObject gameObjectFromObject = GetGameObjectFromObject(obj1);
            if (gameObjectFromObject == null)
            {
                return false;
            }
            GameObject gameObjectFromObject2 = GetGameObjectFromObject(obj2);
            if (gameObjectFromObject2 == null)
            {
                return false;
            }
            if (EditorUtility.IsPersistent(gameObjectFromObject) || EditorUtility.IsPersistent(gameObjectFromObject2))
            {
                return false;
            }
            if (!gameObjectFromObject.scene.IsValid() || !gameObjectFromObject2.scene.IsValid())
            {
                return false;
            }
            return gameObjectFromObject.scene != gameObjectFromObject2.scene;
        }
        
        private static GameObject GetGameObjectFromObject(Object obj)
        {
            GameObject gameObject = obj as GameObject;
            if (gameObject == null && obj is Component)
            {
                gameObject = ((Component)obj).gameObject;
            }
            return gameObject;
        }

        
        protected override void OnRenderProperty(Rect position, PropertyName exposedPropertyNameString,
            UnityEngine.Object currentReferenceValue, SerializedProperty exposedPropertyDefault,
            SerializedProperty exposedPropertyName, ExposedPropertyMode mode,
            IExposedPropertyTable exposedPropertyTable)
        {
            Type objType = Restriction;
            EditorGUI.BeginChangeCheck();
            UnityEngine.Object @object =
                EditorGUI.ObjectField(position, currentReferenceValue, objType, exposedPropertyTable != null);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            if (mode == ExposedPropertyMode.DefaultValue)
            {
                if (!EditorUtility.IsPersistent(exposedPropertyDefault.serializedObject.targetObject) ||
                    @object == null || EditorUtility.IsPersistent(@object))
                {
                    if (!CheckForCrossSceneReferencing(exposedPropertyDefault.serializedObject.targetObject,
                        @object))
                    {
                        exposedPropertyDefault.objectReferenceValue = @object;
                    }
                }
                else
                {
                    string text = GUID.Generate().ToString();
                    exposedPropertyNameString = new PropertyName(text);
                    exposedPropertyName.stringValue = text;
                    Undo.RecordObject(exposedPropertyTable as UnityEngine.Object, "Set Exposed Property");
                    exposedPropertyTable.SetReferenceValue(exposedPropertyNameString, @object);
                }
            }
            else
            {
                Undo.RecordObject(exposedPropertyTable as UnityEngine.Object, "Set Exposed Property");
                exposedPropertyTable.SetReferenceValue(exposedPropertyNameString, @object);
            }
        }

        protected override void PopulateContextMenu(GenericMenu menu, OverrideState overrideState,
            IExposedPropertyTable exposedPropertyTable, SerializedProperty exposedName, SerializedProperty defaultValue)
        {
            PropertyName propertyName = new PropertyName(exposedName.stringValue);
            OverrideState currentOverrideState;
            UnityEngine.Object currentValue = Resolve(new PropertyName(exposedName.stringValue), exposedPropertyTable,
                defaultValue.objectReferenceValue, out currentOverrideState);
            if (overrideState == OverrideState.DefaultValue)
            {
                menu.AddItem(new GUIContent(ExposePropertyContent.text), on: false, delegate
                {
                    GUID gUID = GUID.Generate();
                    exposedName.stringValue = gUID.ToString();
                    exposedName.serializedObject.ApplyModifiedProperties();
                    PropertyName id = new PropertyName(exposedName.stringValue);
                    Undo.RecordObject(exposedPropertyTable as UnityEngine.Object, "Set Exposed Property");
                    exposedPropertyTable.SetReferenceValue(id, currentValue);
                }, null);
            }
            else
            {
                menu.AddItem(UnexposePropertyContent, on: false, delegate
                {
                    exposedName.stringValue = "";
                    exposedName.serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(exposedPropertyTable as UnityEngine.Object, "Clear Exposed Property");
                    exposedPropertyTable.ClearReferenceValue(propertyName);
                }, null);
            }
        }
    }

    public abstract class BaseExposedPropertyDrawer : PropertyDrawer
    {
        protected enum ExposedPropertyMode
        {
            DefaultValue,
            Named,
            NamedGUID
        }

        protected enum OverrideState
        {
            DefaultValue,
            MissingOverride,
            Overridden
        }

        private static float kDriveWidgetWidth = 18f;

        private static GUIStyle kDropDownStyle = null;

        private static Color kMissingOverrideColor = new Color(1f, 0.11f, 0.11f, 1f);

        protected readonly GUIContent ExposePropertyContent = EditorGUIUtility.TrTextContent("Expose Property");

        protected readonly GUIContent UnexposePropertyContent = EditorGUIUtility.TrTextContent("Unexpose Property");

        protected readonly GUIContent NotFoundOn = EditorGUIUtility.TrTextContent("not found on");

        protected readonly GUIContent OverridenByContent = EditorGUIUtility.TrTextContent("Overridden by ");

        private GUIContent m_ModifiedLabel = new GUIContent();

        public BaseExposedPropertyDrawer()
        {
            if (kDropDownStyle == null)
            {
                kDropDownStyle = "ShurikenDropdown";
            }
        }

        private static ExposedPropertyMode GetExposedPropertyMode(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return ExposedPropertyMode.DefaultValue;
            }

            if (GUID.TryParse(propertyName, out GUID _))
            {
                return ExposedPropertyMode.NamedGUID;
            }

            return ExposedPropertyMode.Named;
        }

        protected IExposedPropertyTable GetExposedPropertyTable(SerializedProperty property)
        {
            Object context = property.serializedObject.context;
            return context as IExposedPropertyTable;
        }

        protected abstract void OnRenderProperty(Rect position, PropertyName exposedPropertyNameString,
            Object currentReferenceValue, SerializedProperty exposedPropertyDefault,
            SerializedProperty exposedPropertyName, ExposedPropertyMode mode, IExposedPropertyTable exposedProperties);

        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            SerializedProperty serializedProperty = prop.FindPropertyRelative("defaultValue");
            SerializedProperty serializedProperty2 = prop.FindPropertyRelative("exposedName");
            string stringValue = serializedProperty2.stringValue;
            ExposedPropertyMode exposedPropertyMode = GetExposedPropertyMode(stringValue);
            Rect rect = position;
            rect.xMax -= kDriveWidgetWidth;
            Rect position2 = position;
            position2.x = rect.xMax;
            position2.width = kDriveWidgetWidth;
            IExposedPropertyTable exposedPropertyTable = GetExposedPropertyTable(prop);
            bool flag = exposedPropertyTable != null;
            PropertyName propertyName = new PropertyName(stringValue);
            OverrideState currentOverrideState = OverrideState.DefaultValue;
            Object currentReferenceValue = Resolve(propertyName, exposedPropertyTable,
                serializedProperty.objectReferenceValue, out currentOverrideState);
            Color color = GUI.color;
            //bool boldDefaultFont = false;//EditorGUIUtility.GetBoldDefaultFont();
            Rect position3 = DrawLabel(flag, currentOverrideState, label, position, exposedPropertyTable, stringValue,
                serializedProperty2, serializedProperty);
            if (exposedPropertyMode == ExposedPropertyMode.DefaultValue ||
                exposedPropertyMode == ExposedPropertyMode.NamedGUID)
            {
                OnRenderProperty(position3, propertyName, currentReferenceValue, serializedProperty,
                    serializedProperty2, exposedPropertyMode, exposedPropertyTable);
            }
            else
            {
                position3.width /= 2f;
                EditorGUI.BeginChangeCheck();
                stringValue = EditorGUI.TextField(position3, stringValue);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedProperty2.stringValue = stringValue;
                }

                position3.x += position3.width;
                OnRenderProperty(position3, new PropertyName(stringValue), currentReferenceValue, serializedProperty,
                    serializedProperty2, exposedPropertyMode, exposedPropertyTable);
            }

            GUI.color = color;
            //EditorGUIUtility.SetBoldDefaultFont(boldDefaultFont);
            if (flag && GUI.Button(position2, GUIContent.none, kDropDownStyle))
            {
                GenericMenu genericMenu = new GenericMenu();
                PopulateContextMenu(genericMenu, currentOverrideState, exposedPropertyTable, serializedProperty2,
                    serializedProperty);
                genericMenu.ShowAsContext();
                Event.current.Use();
            }
        }

        private Rect DrawLabel(bool showContextMenu, OverrideState currentOverrideState, GUIContent label,
            Rect position, IExposedPropertyTable exposedPropertyTable, string exposedNameStr,
            SerializedProperty exposedName, SerializedProperty defaultValue)
        {
            if (showContextMenu)
            {
                position.xMax -= kDriveWidgetWidth;
            }

            //EditorGUIUtility.SetBoldDefaultFont(currentOverrideState != OverrideState.DefaultValue);
            m_ModifiedLabel.text = label.text;
            m_ModifiedLabel.tooltip = label.tooltip;
            m_ModifiedLabel.image = label.image;
            if (!string.IsNullOrEmpty(m_ModifiedLabel.tooltip))
            {
                m_ModifiedLabel.tooltip += "\n";
            }

            if (currentOverrideState == OverrideState.MissingOverride)
            {
                GUI.color = kMissingOverrideColor;
                GUIContent modifiedLabel = m_ModifiedLabel;
                modifiedLabel.tooltip = modifiedLabel.tooltip + label.text + " " + NotFoundOn.text + " " +
                                        exposedPropertyTable + ".";
            }
            else if (currentOverrideState == OverrideState.Overridden && exposedPropertyTable != null)
            {
                GUIContent modifiedLabel = m_ModifiedLabel;
                modifiedLabel.tooltip = modifiedLabel.tooltip + OverridenByContent.text + exposedPropertyTable + ".";
            }

            Rect result = EditorGUI.PrefixLabel(position, m_ModifiedLabel, EditorStyles.boldLabel);
            if (exposedPropertyTable != null && Event.current.type == EventType.ContextClick &&
                position.Contains(Event.current.mousePosition))
            {
                GenericMenu genericMenu = new GenericMenu();
                PopulateContextMenu(genericMenu,
                    (!string.IsNullOrEmpty(exposedNameStr)) ? OverrideState.Overridden : OverrideState.DefaultValue,
                    exposedPropertyTable, exposedName, defaultValue);
                genericMenu.ShowAsContext();
            }

            return result;
        }

        protected Object Resolve(PropertyName exposedPropertyName, IExposedPropertyTable exposedPropertyTable,
            Object defaultValue, out OverrideState currentOverrideState)
        {
            Object @object = null;
            bool idValid = false;
            bool flag = !PropertyName.IsNullOrEmpty(exposedPropertyName);
            currentOverrideState = OverrideState.DefaultValue;
            if (exposedPropertyTable != null)
            {
                @object = exposedPropertyTable.GetReferenceValue(exposedPropertyName, out idValid);
                if (idValid)
                {
                    currentOverrideState = OverrideState.Overridden;
                }
                else if (flag)
                {
                    currentOverrideState = OverrideState.MissingOverride;
                }
            }

            return (currentOverrideState == OverrideState.Overridden) ? @object : defaultValue;
        }

        protected abstract void PopulateContextMenu(GenericMenu menu, OverrideState overrideState,
            IExposedPropertyTable exposedPropertyTable, SerializedProperty exposedName,
            SerializedProperty defaultValue);
    }
}