using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace Ruccho.Utilities
{
    [Serializable]
    public class ExposedInvokableCall
    {
        [SerializeField] private ExposedReference<UnityEngine.Object> target = default;
        [SerializeField] private string componentTypeName = default;
        [SerializeField] private string methodName = default;

        [SerializeReference] private ExposedInvokableCallArgumentBase[] arguments = default;

        private UnityEngine.Object cachedContainer;
        private Delegate cachedDelegate;

        public void Invoke(IExposedPropertyTable resolver)
        {
            var targetContainer = target.Resolve(resolver);

            if (!targetContainer)
            {
                throw new NullReferenceException("Failed to resolve ExposedReference.");
            }
            
            if (cachedDelegate != null && targetContainer == cachedContainer)
            {
                //Use cached delegate
                cachedDelegate.DynamicInvoke(arguments.Select(a => a.GetValue(resolver)).ToArray());
                return;
            }

            UnityEngine.Object targetObj = null;
            if (targetContainer.GetType().FullName != componentTypeName)
            {
                if (targetContainer is GameObject targetGameObject)
                {
                    targetObj = targetGameObject.GetComponent(componentTypeName);
                }
                else if (targetContainer is Component targetComponent)
                {
                    targetObj = targetComponent.GetComponent(componentTypeName);
                }
            }
            else targetObj = targetContainer;

            var argumentValues = arguments.Select(a =>
            {
                return a.GetValue(resolver);
            }).ToArray();


            var method = FindMethod(targetObj);

            if (method == null)
            {
                throw new NullReferenceException("Failed to find target method.");
            }

            //Generate Generic Action Type

            var argumentTypes = arguments.Select(a => a.ValueType).ToArray();
            Delegate del = null;
            Type genericDef = GetActionGenericDefinitionType(argumentTypes.Length);
            if (argumentTypes.Length > 0)
            {
                Type constructed = genericDef.MakeGenericType(argumentTypes);
                del = method.CreateDelegate(constructed, targetObj);
            }
            else
            {
                del = method.CreateDelegate(genericDef, targetObj);
            }

            //Execute
            cachedContainer = targetContainer;
            cachedDelegate = del;

            del.DynamicInvoke(argumentValues);
        }

        private MethodInfo FindMethod(UnityEngine.Object targetObject)
        {
            var argumentTypes = arguments.Select(a => a.ValueType).ToArray();

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


        private static Type GetActionGenericDefinitionType(int argCount)
        {
            switch (argCount)
            {
                case 0:
                    return typeof(Action);
                case 1:
                    return typeof(Action<>);
                case 2:
                    return typeof(Action<,>);
                case 3:
                    return typeof(Action<,,>);
                case 4:
                    return typeof(Action<,,,>);
                case 5:
                    return typeof(Action<,,,,>);
                case 6:
                    return typeof(Action<,,,,,>);
                case 7:
                    return typeof(Action<,,,,,,>);
                case 8:
                    return typeof(Action<,,,,,,,>);
                case 9:
                    return typeof(Action<,,,,,,,,>);
                case 10:
                    return typeof(Action<,,,,,,,,,>);
                case 11:
                    return typeof(Action<,,,,,,,,,,>);
                case 12:
                    return typeof(Action<,,,,,,,,,,,>);
                case 13:
                    return typeof(Action<,,,,,,,,,,,,>);
                case 14:
                    return typeof(Action<,,,,,,,,,,,,,>);
                case 15:
                    return typeof(Action<,,,,,,,,,,,,,,>);
                case 16:
                    return typeof(Action<,,,,,,,,,,,,,,,>);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [Serializable]
    public abstract class ExposedInvokableCallArgumentBase
    {
        public abstract Type ValueType { get; }
        public abstract object GetValue(IExposedPropertyTable resolver);
    }


    //[Serializable]
    public class ExposedInvokableCallArgument<T> : ExposedInvokableCallArgumentBase
    {
        [SerializeField] private string argumentLabel;
        [SerializeField] protected T value;
        public override Type ValueType => typeof(T);
        public override object GetValue(IExposedPropertyTable resolver) => value;
    }

    [Serializable]
    public class ExposedInvokableCallArgumentObject : ExposedInvokableCallArgument<UnityEngine.Object>
    {
        [SerializeField] public string objectTypeName;
        [SerializeField] public string objectTypeAssemblyName;

        private Type valueType;
        public override Type ValueType
        {
            get
            {
                if (valueType != null) return valueType;
                var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == objectTypeAssemblyName);
                if (asm == default) return typeof(UnityEngine.Object);
                valueType = asm.GetType(objectTypeName);
                return valueType;
            }
        }
    }
    
    
    [Serializable]
    public class ExposedInvokableCallArgumentExposedObject : ExposedInvokableCallArgument<ExposedReference<UnityEngine.Object>>
    {
        [SerializeField] public ExposedReference<UnityEngine.Object> coverValue;
        [SerializeField] public string objectTypeName;
        [SerializeField] public string objectTypeAssemblyName;

        private Type valueType;
        public override Type ValueType
        {
            get
            {
                if (valueType != null) return valueType;
                var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == objectTypeAssemblyName);
                if (asm == default) return typeof(UnityEngine.Object);
                valueType = asm.GetType(objectTypeName);
                return valueType;
            }
        }

        public override object GetValue(IExposedPropertyTable resolver)
        {
            return coverValue.Resolve(resolver);
        }
    }

    [Serializable]
    public class ExposedInvokableCallArgumentInt : ExposedInvokableCallArgument<int>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentFloat : ExposedInvokableCallArgument<float>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentDouble : ExposedInvokableCallArgument<double>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentBool : ExposedInvokableCallArgument<bool>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentString : ExposedInvokableCallArgument<string>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentVector2 : ExposedInvokableCallArgument<Vector2>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentVector3 : ExposedInvokableCallArgument<Vector3>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentVector4 : ExposedInvokableCallArgument<Vector4>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentRect : ExposedInvokableCallArgument<Rect>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentQuaternion : ExposedInvokableCallArgument<Quaternion>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentMatrix4x4 : ExposedInvokableCallArgument<Matrix4x4>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentColor : ExposedInvokableCallArgument<Color>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentColor32 : ExposedInvokableCallArgument<Color32>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentLayerMask : ExposedInvokableCallArgument<LayerMask>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentAnimationCurve : ExposedInvokableCallArgument<AnimationCurve>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentGradient : ExposedInvokableCallArgument<Gradient>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentRectOffset : ExposedInvokableCallArgument<RectOffset>
    {
    }

    [Serializable]
    public class ExposedInvokableCallArgumentGUIStyle : ExposedInvokableCallArgument<GUIStyle>
    {
    }
}