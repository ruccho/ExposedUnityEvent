using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Ruccho.Utilities.Editors
{
    [CustomPropertyDrawer(typeof(ExposedUnityEvent))]
    public class ExposedUnityEventPropertyDrawer : PropertyDrawer
    {
        private class State
        {
            public ReorderableList reorderableList;
            public SerializedProperty sourceProperty;

            public State(ReorderableList reorderableList, SerializedProperty sourceProperty)
            {
                this.reorderableList = reorderableList;
                this.sourceProperty = sourceProperty;
            }
        }

        private Dictionary<string, State> stateStore = new Dictionary<string, State>();
        private SerializedProperty currentCallsProperty;
        private ReorderableList currentReorderableList;
        private int currentSelectedIndex;
        private string labelText;

        private void RestoreState(SerializedProperty property)
        {
            stateStore.TryGetValue(property.propertyPath, out var state);

            var list = state?.reorderableList;

            if (state != null && state.sourceProperty.serializedObject == property.serializedObject)
            {
                currentCallsProperty = list.serializedProperty;
            }
            else
            {
                currentCallsProperty = property.FindPropertyRelative("calls");
                list = new ReorderableList(property.serializedObject, currentCallsProperty,
                    false, true, true, true)
                {
                    drawElementCallback = DrawElementCallback,
                    elementHeightCallback = ElementHeightCallback,
                    onAddCallback = OnAddCallback,
                    onSelectCallback = OnSelectCallback,
                    onReorderCallback = OnReorderCallback,
                    onRemoveCallback = OnRemoveCallback,
                    drawHeaderCallback = DrawHeaderCallback
                };
                stateStore[property.propertyPath] = new State(list, property);
            }


            currentReorderableList = list;
            currentSelectedIndex = 0;
        }

        private void DrawHeaderCallback(Rect rect)
        {
            rect.height = 18f;
            string text = (string.IsNullOrEmpty(labelText) ? "Event" : labelText); // + GetEventParams(m_DummyEvent);
            GUI.Label(rect, text);
        }

        private void OnRemoveCallback(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            currentSelectedIndex = list.index;
        }


        private void OnReorderCallback(ReorderableList list)
        {
            currentSelectedIndex = list.index;
        }

        private void OnSelectCallback(ReorderableList list)
        {
            var callProp = currentCallsProperty.GetArrayElementAtIndex(list.index);
            currentReorderableList.elementHeight = EditorGUI.GetPropertyHeight(callProp);
            currentSelectedIndex = list.index;
        }

        private void OnAddCallback(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoAddButton(list);

            currentSelectedIndex = list.index;
            SerializedProperty arrayElementAtIndex = currentCallsProperty.GetArrayElementAtIndex(list.index);
            SerializedProperty serializedProperty = arrayElementAtIndex.FindPropertyRelative("target");
            SerializedProperty serializedProperty2 = arrayElementAtIndex.FindPropertyRelative("componentTypeName");
            SerializedProperty serializedProperty3 = arrayElementAtIndex.FindPropertyRelative("methodName");
            SerializedProperty serializedProperty4 = arrayElementAtIndex.FindPropertyRelative("arguments");
            serializedProperty.FindPropertyRelative("exposedName").stringValue = "";
            serializedProperty.FindPropertyRelative("defaultValue").objectReferenceValue = null;
            serializedProperty2.stringValue = "";
            serializedProperty3.stringValue = "";

            serializedProperty4.ClearArray();
            currentCallsProperty.serializedObject.ApplyModifiedProperties();
        }

        private float ElementHeightCallback(int index)
        {
            //if (currentCallsProperty == null) return 0;
            var callProp = currentCallsProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(callProp) + 5 * 2;
        }

        private void DrawElementCallback(Rect rect, int index, bool isactive, bool isfocused)
        {
            rect.height -= 5f * 2;
            rect.y += 5;
            rect.x += 10;
            rect.width -= 10f;
            var callProp = currentCallsProperty.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, callProp);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            RestoreState(property);
            labelText = label.text;
            float result = 0f;
            if (currentReorderableList != null)
            {
                result = currentReorderableList.GetHeight();
            }


            return result;
/*
            var line = EditorGUIUtility.singleLineHeight;
            var unit = line + EditorGUIUtility.standardVerticalSpacing;

            var callsProp = property.FindPropertyRelative("calls");
            var callsPropSize = callsProp.arraySize;
            float height = unit;
            for (int i = 0; i < callsPropSize; i++)
            {
                var callProp = callsProp.GetArrayElementAtIndex(i);
                height += EditorGUI.GetPropertyHeight(callProp);
            }

            return height;
            */
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            RestoreState(property);
            if (currentReorderableList != null)
            {
                currentReorderableList.DoList(position);
            }

            /*
            var context = property.serializedObject.targetObject;
            if (property.serializedObject.context != null)
            {
                context = property.serializedObject.context;
                
            }

            OnGUI(position, property, label, context);
            */
        }

        private void OnGUI(Rect position, SerializedProperty property, GUIContent label, Object context)
        {
            RestoreState(property);
            if (currentReorderableList != null)
            {
                currentReorderableList.DoList(position);
            }

            //currentReorderableList.serializedProperty.serializedObject.ApplyModifiedProperties();
            return;
/*
            var line = EditorGUIUtility.singleLineHeight;
            var unit = line + EditorGUIUtility.standardVerticalSpacing;

            position.height = line;


            var callsProp = contextedProperty.FindPropertyRelative("calls");
            var callsPropSize = callsProp.arraySize;
            for (int i = 0; i < callsPropSize; i++)
            {
                var callProp = callsProp.GetArrayElementAtIndex(i);
                EditorGUI.PropertyField(position, callProp);

                position.y += EditorGUI.GetPropertyHeight(callProp);
            }*/
        }
    }
}