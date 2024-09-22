using System;
using System.Collections;
using System.Collections.Generic;
using MyBox;
using Unity.VisualScripting;
using UnityEngine;

public class PrefabPool : MonoBehaviour
{
	public GameObject Prefab;

	private PoolItem prefabItem;
	private Stack<PoolItem> pool = new ();
	
	private void Awake()
	{
		if (Prefab != null)
		{
			Init(Prefab);
		}
	}

	public void Init(GameObject prefab)
	{
		Prefab = prefab;
		prefabItem = Prefab.GetOrAddComponent<PoolItem>();
	}

	private void ReturnToPool(PoolItem item)
	{
		item.gameObject.SetActive(false);
		pool.Push(item);
	}

	public PoolItem GetNewItem(Vector3 position, Quaternion rotation)
	{
		PoolItem item = pool.Count == 0 ? CreateNewItem() : pool.Pop();
		item.gameObject.SetActive(true);
		item.transform.SetPositionAndRotation(position, rotation);
		return item;
	}
	
	private PoolItem CreateNewItem()
	{
		PoolItem item = Instantiate(prefabItem);
		item.Pool = this;
		return item;
	}
	
	public class PoolItem : MonoBehaviour
	{
		public PrefabPool Pool;
		
		public event Action<PoolItem> OnReturnToPool;
	
		public void ReturnToPool()
		{
			Pool.ReturnToPool(this);
			OnReturnToPool?.Invoke(this);
		}
	}
}

public static class PoolExtensions
{
	public static void ReturnToPool(this GameObject go)
	{
		go.GetComponent<PrefabPool.PoolItem>().ReturnToPool();
	}
}

