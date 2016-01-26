using UnityEngine;
using System.Collections;

public class Unit : MonoBehaviour {

	public bool Selected{ 
		get { return _selected;} 
		set { _selected = value; 
			if (value){
				this.gameObject.GetComponent<MeshRenderer>().material = (Material)Material.Instantiate(Resources.Load("Materials/Red"));
			} else {
				this.gameObject.GetComponent<MeshRenderer>().material = (Material)Material.Instantiate(Resources.Load("Materials/Green"));
			}
		}
	}

	private bool _selected = false;

	public int Id = 0;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
