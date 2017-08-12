using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

public class Singleton<T> where T : class
{
    static T s_instance;

    public static T instance
    {
        get
        {
            if (s_instance == null)
            {
                Type type = typeof(T);
                ConstructorInfo ctor;
                ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                              null, new Type[0], new ParameterModifier[0]);
                s_instance = (T)ctor.Invoke(new object[0]);
            }
            return s_instance;
        }
    }

    protected Singleton()
    {
        init();
    }

    protected virtual void init()
    {

    }

    static bool testPubConstructor()
    {
        if (Application.isEditor)
        {
            Type type = typeof(T);
            ConstructorInfo ctor;
            ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new Type[0], new ParameterModifier[0]);
            if (ctor != null)
                Debuger.LogError("Singleton class " + type.FullName + "have Public Constructor function");
        }
        return true;
    }

    
}

public abstract class SingletonMonoBehavior<T> : MonoBehaviour where T : SingletonMonoBehavior<T>
{
    private static T s_instance = null;
    private static int instance_count = 0;
    public static T instance
    {
        get
        {
            return s_instance;
        }
    }

    public void Awake()
    {
        if (s_instance == null)
        {          
            s_instance = this as T;
            s_instance.Init();
        }
        else
        {
            Destroy(this);
        }

        ++instance_count;
    }

    void OnDestroy()
    {
        --instance_count;
        if (instance_count == 0)
            s_instance = null;
    }

    protected virtual void Init()
    {

    }
}

public abstract class SingletonMonoBehaviorNoDestroy<T> : MonoBehaviour where T : SingletonMonoBehaviorNoDestroy<T>
{
    private static T s_instance = null;
    public static T instance
    {
        get
        {
            return s_instance;
        }
    }

    void Awake()
    {
        if (s_instance == null)
        {
            DontDestroyOnLoad(this.gameObject);
            s_instance = this as T;
            s_instance.Init();
        }
        else
        {
            Destroy(this);
        }
    }

    protected virtual void Init()
    {

    }

    public static void ReleaseInstance()
    {
        if (s_instance != null)
        {
            Destroy(s_instance);
            s_instance = null;
        }
    }
}

public abstract class SingletonMonoBehaviorAutoCreateNoDestroy<T> : MonoBehaviour where T : SingletonMonoBehaviorAutoCreateNoDestroy<T>
{
    private static T s_instance = null;

    public static T instance
    {
        get
        {
            if (s_instance != null)
                return s_instance;
            CreateInstance();
            return s_instance;
        }
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (s_instance == null)
        {
            s_instance = this as T;
            s_instance.Init();
        }
        else
            Destroy(this);
    }

    protected virtual void Init()
    {

    }

    public static void CreateInstance()
    {
        if (s_instance != null)
            return;

        GameObject singletonObject = SingletonGameObject.getObject();
        if (singletonObject == null)
            return;
        DontDestroyOnLoad(singletonObject);
 
        T[] objList = GameObject.FindObjectsOfType(typeof(T)) as T[];
        if (objList.Length == 0)
        {
            singletonObject.AddComponent<T>();
        }
        else if(objList.Length > 1)
        {
            Debuger.LogError("You have more than one " + typeof(T).Name + " in the scene. You only need 1, it's a singleton!");
            foreach (T item in objList)
            {
                Destroy(item);
            }
        }
    }

    public static void ReleaseInstance()
    {
        if (s_instance != null)
        {
            Destroy(s_instance);
            s_instance = null;
        }
    }
}

class SingletonGameObject
{
    const string s_objName = "SingletonObject";
    static GameObject s_SingletonObject = null;

    public static GameObject getObject()
    {
        if (s_SingletonObject == null)
        {
            s_SingletonObject = GameObject.Find(s_objName);
            if (s_SingletonObject == null)
            {
                Debuger.Log("CreateInstance");
                s_SingletonObject = new GameObject(s_objName);
            }
        }
        return s_SingletonObject;
    }

    public static void destroyObject()
    {
        if (s_SingletonObject != null)
        {
            GameObject.DestroyImmediate(s_SingletonObject);
            s_SingletonObject = null;
        }
    }
}