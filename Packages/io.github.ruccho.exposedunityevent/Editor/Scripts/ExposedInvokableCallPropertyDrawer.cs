using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Ruccho.Utilities.Editors
{
    [CustomPropertyDrawer(typeof(ExposedInvokableCall))]
    public class ExposedInvokableCallPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var line = EditorGUIUtility.singleLineHeight;
            var unit = line + EditorGUIUtility.standardVerticalSpacing;

            var argumentsProp = property.FindPropertyRelative("arguments");

            float height = unit;

            //Arguments slot
            int argumentsCount = argumentsProp.arraySize;
            for (int i = 0; i < argumentsCount; i++)
            {
                var argumentProp = argumentsProp.GetArrayElementAtIndex(i);
                var argumentValueProp = argumentProp.FindPropertyRelative("value");
                if (argumentValueProp == null) continue;

                if (argumentValueProp.propertyType == SerializedPropertyType.ObjectReference)
                {
                    height += unit;
                }
                else
                {
                    float h = EditorGUI.GetPropertyHeight(argumentValueProp, false);
                    height += h + EditorGUIUtility.standardVerticalSpacing;
                }
            }


            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var line = EditorGUIUtility.singleLineHeight;
            var unit = line + EditorGUIUtility.standardVerticalSpacing;

            var targetExposedReferenceProp = property.FindPropertyRelative("target");

            var target = targetExposedReferenceProp.exposedReferenceValue;

            var methodNameProp = property.FindPropertyRelative("methodName");
            var methodName = methodNameProp.stringValue;

            var argumentsProp = property.FindPropertyRelative("arguments");
            int argumentsCount = argumentsProp.arraySize;
            /*
            if (target != null)
            {
                Type[] argumentTypes = new Type[argumentsCount];
                for (int i = 0; i < argumentsCount; i++)
                {
                    var argumentProp = argumentsProp.GetArrayElementAtIndex(i);
                    var argumentValueProp = argumentProp.FindPropertyRelative("value");
                    if (argumentValueProp == null) continue;

                    string[] typeInfo = argumentProp.managedReferenceFullTypename.Split(' ');
                    //Check type

                    Type argumentContainerType = typeof(object);
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        var matched = assembly.GetTypes().FirstOrDefault(t => t.FullName == typeInfo[1]);
                        if (matched != default)
                        {
                            argumentContainerType = matched;
                            break;
                        }
                    }

                    var argumentContainerGenericArgumentTypes = argumentContainerType.BaseType.GetGenericArguments();
                    Type argumentType = null;

                    if (argumentContainerType == typeof(ExposedInvokableCallArgumentObject))
                    {
                        var argumentObjectTypeName = argumentProp.FindPropertyRelative("objectTypeName").stringValue;
                        argumentType = typeof(UnityEngine.Object);
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            var matched = assembly.GetTypes().FirstOrDefault(t => t.FullName == argumentObjectTypeName);
                            if (matched != default)
                            {
                                argumentType = matched;
                                break;
                            }
                        }
                    }
                    else
                    {
                        argumentType = argumentContainerGenericArgumentTypes[0];
                    }

                    argumentTypes[i] = argumentType;
                }

                //var matchedMethod = FindMethod(target, methodName, argumentTypes);
            }
*/
            //Debug.Log(matchedMethod != null);


            var r = position;
            r.height = line;

            //Target and method slot

            EditorGUI.BeginChangeCheck();
            
            r.width = position.width * 0.3f;
            EditorGUI.PropertyField(r, targetExposedReferenceProp, new GUIContent(""));
            r.x += position.width * 0.3f;
            r.width = position.width * 0.7f;

            if (GUI.Button(r, new GUIContent(methodName), EditorStyles.popup))
            {
                var menu = BuildGenericMenu(property, target, data => { });
                menu.DropDown(r);
            }
            else
            {
            }

            r.y += unit;

            //Arguments slot
            EditorGUI.indentLevel++;
            r.width = position.width;
            r.x = position.x;
            argumentsCount = argumentsProp.arraySize;
            for (int i = 0; i < argumentsCount; i++)
            {
                var argumentProp = argumentsProp.GetArrayElementAtIndex(i);
                var argumentValueProp = argumentProp.FindPropertyRelative("value");
                if (argumentValueProp == null) continue;
                
                var labelText = argumentProp.FindPropertyRelative("argumentLabel").stringValue;

                if (argumentValueProp.propertyType == SerializedPropertyType.ObjectReference)
                {
                    r.height = line;

                    var objectTypeName = argumentProp.FindPropertyRelative("objectTypeName").stringValue;
                    Type objectType = typeof(UnityEngine.Object);
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        var matched = assembly.GetTypes().FirstOrDefault(t => t.FullName == objectTypeName);
                        if (matched != default)
                        {
                            objectType = matched;
                            break;
                        }
                    }

                    argumentValueProp.objectReferenceValue = EditorGUI.ObjectField(r, new GUIContent(labelText),
                        argumentValueProp.objectReferenceValue, objectType, true);

                    r.y += unit;
                }
                else if (argumentProp.type == "managedReference<ExposedInvokableCallArgumentExposedObject>")
                {
                    var argumentObjectTypeName = argumentProp.FindPropertyRelative("objectTypeName").stringValue;
                    var argumentObjectTypeAssemblyName = argumentProp.FindPropertyRelative("objectTypeAssemblyName").stringValue;
                    
                    var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == argumentObjectTypeAssemblyName);
                    
                    Type valueType = asm.GetType(argumentObjectTypeName);
                    var coverValueProp = argumentProp.FindPropertyRelative("coverValue");
                    float h = EditorGUI.GetPropertyHeight(coverValueProp);
                    r.height = h;
                    new TypeRestrictedExposedReferenceDrawer(valueType).OnGUI(r, coverValueProp, new GUIContent(labelText));
                    r.y += h + EditorGUIUtility.standardVerticalSpacing;
                    
                }
                else
                {
                    if (string.IsNullOrEmpty(labelText)) continue;
                    float h = EditorGUI.GetPropertyHeight(argumentValueProp, false);
                    r.height = h;
                    EditorGUI.PropertyField(r, argumentValueProp, new GUIContent(labelText), false);
                    r.y += h + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        private static GenericMenu BuildGenericMenu(SerializedProperty property, UnityEngine.Object target,
            GenericMenu.MenuFunction2 clearEventFunction)
        {
            UnityEngine.Object @object = target;
            if (@object is Component)
            {
                @object = (target as Component).gameObject;
            }

            SerializedProperty serializedProperty = property.FindPropertyRelative("methodName");
            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("No Function"), string.IsNullOrEmpty(serializedProperty.stringValue),
                clearEventFunction, 0);
            if (@object == null)
            {
                return genericMenu;
            }

            genericMenu.AddSeparator("");


            GeneratePopUpForType(property, genericMenu, @object);

            if (@object is GameObject)
            {
                Component[] components = (@object as GameObject).GetComponents<Component>();
                //同名のComponentはひとつにまとめる
                List<string> list = (from c in components
                    where c != null
                    select c.GetType().Name
                    into x
                    group x by x
                    into g
                    where g.Count() > 1
                    select g.Key).ToList();
                Component[] array = components;
                foreach (Component component in array)
                {
                    if (!(component == null))
                    {
                        GeneratePopUpForType(property, genericMenu, component);
                    }
                }
            }

            return genericMenu;
        }

        private static void GeneratePopUpForType(SerializedProperty property, GenericMenu menu,
            UnityEngine.Object target)
        {
            Type t = target.GetType();
            var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            List<MethodInfo> compatibleMethods = new List<MethodInfo>();

            List<Type> containerTypes = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                containerTypes.AddRange(
                    assembly.GetTypes().Where(
                            t_ => t_.IsSubclassOf(typeof(ExposedInvokableCallArgumentBase)))
                        .Where(t_ => t_.BaseType.GenericTypeArguments.Length > 0)
                        .Select(t_ =>
                        {
                            var genericTypeArguments = t_.BaseType.GenericTypeArguments;
                            return genericTypeArguments[0];
                        }));
            }

            foreach (var m in methods)
            {
                var parameters = m.GetParameters();

                //Getterはスキップ
                if (m.Name.StartsWith("get_"))
                {
                    continue;
                }

                //シリアライズ可能かチェック

                bool serializableOnly = true;
                foreach (var p in parameters)
                {
                    if (p.ParameterType.IsSubclassOf(typeof(UnityEngine.Object))) continue;

                    if (!containerTypes.Contains(p.ParameterType))
                    {
                        serializableOnly = false;
                        break;
                    }

                    /*
                    var isUnityObject = p.ParameterType.IsSubclassOf(typeof(UnityEngine.Object));
                    var isObject = p.ParameterType == typeof(System.Object);
                    var hasSerializableAttribute = (p.ParameterType.Attributes & TypeAttributes.Serializable) != 0;
                    var isAbstract = p.ParameterType.IsAbstract;
                    var isGeneric = p.ParameterType.IsGenericType;

                    if (isUnityObject) continue;

                    if (isObject || !hasSerializableAttribute || isAbstract || isGeneric)
                    {
                        //Unserializable
                        serializableOnly = false;
                        break;
                    }*/
                }

                if (!serializableOnly)
                {
                    continue;
                }

                compatibleMethods.Add(m);
            }

            var grouped = compatibleMethods.GroupBy(m => m.Name).OrderBy(g => g.Key);

            foreach (var g in grouped)
            {
                foreach (var m in g)
                {
                    var parameters = m.GetParameters();
                    string parametersText = string.Join(", ", parameters.Select(p => p.ParameterType.Name));

                    string label = "";
                    if (g.Count() == 1)
                    {
                        label = $"{t.Name}/{m.Name}({parametersText})";
                    }
                    else
                    {
                        label = $"{t.Name}/{m.Name}/{m.Name}({parametersText})";
                    }


                    menu.AddItem(new GUIContent(label), false,
                        (methodInfo) =>
                        {
                            MethodInfo method = (MethodInfo) methodInfo;
                            SetMethod(property, target, method);
                        }, m);
                }
            }
        }

        private static bool SetMethod(SerializedProperty property, UnityEngine.Object target, MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();

            var argumentsProp = property.FindPropertyRelative("arguments");
            int sourceSize = argumentsProp.arraySize;
            argumentsProp.arraySize = parameters.Length;

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var elementProp = argumentsProp.GetArrayElementAtIndex(i);
                if (i >= sourceSize)
                {
                    elementProp.managedReferenceValue = null;
                    //elementProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }

                //Get target container type
                var genericArgumentType = parameter.ParameterType;
                if (genericArgumentType.IsSubclassOf(typeof(UnityEngine.Object)))
                    genericArgumentType = typeof(UnityEngine.Object);
                var containerGenericType =
                    typeof(ExposedInvokableCallArgument<>).MakeGenericType(genericArgumentType);
                Type containerType = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    containerType = assembly.GetTypes().FirstOrDefault(t => t.IsSubclassOf(containerGenericType));
                    if (containerType != null)
                    {
                        break;
                    }
                }

                if (containerType == null)
                {
                    Debug.LogError("Argument container type was not found.");
                    return false;
                }

                //Filled with instance
                bool typeMatched = false;
                if (elementProp.hasVisibleChildren)
                {
                    //Check type
                    var typeInfo = elementProp.managedReferenceFullTypename.Split(' ');
                    if (typeInfo.Length == 2)
                    {
                        if (typeInfo[1] == containerType.FullName)
                        {
                            if (genericArgumentType != typeof(UnityEngine.Object))
                            {
                                typeMatched = true;
                            }
                            else
                            {
                                var name = elementProp.FindPropertyRelative("objectTypeName").stringValue;
                                if (name == parameter.ParameterType.FullName)
                                {
                                    typeMatched = true;
                                }
                            }
                        }
                    }
                }

                if (!typeMatched)
                {
                    object argumentInstance;

                    if (genericArgumentType == typeof(UnityEngine.Object))
                    {
                        var parameterType = parameter.ParameterType;
                        var a = new ExposedInvokableCallArgumentExposedObject();
                        a.objectTypeName = parameter.ParameterType.FullName;
                        a.objectTypeAssemblyName = parameter.ParameterType.Assembly.GetName().Name;
                        argumentInstance = a;
                    }
                    else
                    {
                        argumentInstance =
                            Activator.CreateInstance(containerType);
                    }

                    elementProp.managedReferenceValue = argumentInstance;
                }

                var labelProp = elementProp.FindPropertyRelative("argumentLabel");
                labelProp.stringValue = parameter.Name;
            }

            var componentType = target.GetType();
            property.FindPropertyRelative("componentTypeName").stringValue =
                $"{componentType.FullName}";
            property.FindPropertyRelative("methodName").stringValue = methodInfo.Name as string;

            property.serializedObject.ApplyModifiedProperties();
            return true;
        }

        private static MethodInfo FindMethod(UnityEngine.Object targetObject, string methodName, Type[] argumentTypes)
        {
            for (Type type = targetObject.GetType(); type != typeof(object) && type != null; type = type.BaseType)
            {
                MethodInfo method = type.GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, argumentTypes,
                    null);
                if (method != null)
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    bool flag = true;
                    int index = 0;
                    foreach (ParameterInfo parameterInfo in parameters)
                    {
                        flag = argumentTypes[index].IsPrimitive == parameterInfo.ParameterType.IsPrimitive;
                        if (flag)
                            ++index;
                        else
                            break;
                    }

                    if (flag)
                        return method;
                }
            }

            return null;
        }
    }
}