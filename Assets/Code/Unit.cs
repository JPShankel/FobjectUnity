using UnityEngine;
using System.Collections;

public class Unit : MonoBehaviour {

	public bool Selected{ 
		get { 
			return _selected;
		} 
		set {
			_selected = value; 
			if (value){
				gameObject.GetComponent<MeshRenderer>().material = (Material)Material.Instantiate(Resources.Load("Materials/Red"));
			} else {
				gameObject.GetComponent<MeshRenderer>().material = (Material)Material.Instantiate(Resources.Load("Materials/Green"));
			}
		}
	}

	private bool _selected = false;

	public int Id = 0;

	public void BeginDrag() {
		_dragOrigin = gameObject.transform.position;
	}

	public Vector3 DragOrigin { get { return _dragOrigin;} }

	private Vector3 _dragOrigin;

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
