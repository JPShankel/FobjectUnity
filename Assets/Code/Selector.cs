using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Selector : MonoBehaviour {

	public enum State {
		Neutral,
		Moving,
		Selecting
	}
		
	private class Transaction{
		public enum Type {
			ChangeState,
			ToggleSelect,
			DeselectAll,
			MoveObject
		}

		public Type type;
		public State state;
		public Unit unit;
		public Vector3 position;

		public Transaction(Type t,State s) {
			type = t;
			state = s;
		}

		public Transaction(Type t,Unit u) {
			type = t;
			unit = u;
		}

		public Transaction(Type t,Unit u,Vector3 p) {
			type = t;
			unit = u;
			position = p;
		}

		public Transaction(Type t) {
			type = t;
		}
	}

	State _state = State.Neutral;

	Unit[] _units;

	class FuncParams{
		public Vector3 position;
		public Unit unit;
		public Unit[] selected;
		public FuncParams(Vector3 p,Unit u,Unit[] s){
			position = p;
			unit = u;
			selected = s;
		}
	}

	private class MouseFuncMap : Dictionary<State,Func<FuncParams,List<Transaction>>>{};

	MouseFuncMap _mouseDown = new MouseFuncMap();
	MouseFuncMap _mouseUp   = new MouseFuncMap();
	MouseFuncMap _mouseMove = new MouseFuncMap();

	private class TransactionMap : Dictionary<Transaction.Type,Action<Transaction>>{};
	TransactionMap _transactionMap = new TransactionMap();

	// Use this for initialization
	void Start () {

		GameObject root = GameObject.Find("Units");
		_units = root.GetComponentsInChildren<Unit>();

		Enumerable.Range(0,_units.Length)
			.ToList()
			.ForEach(i=>_units[i].Id = i+1);


		_mouseDown[State.Neutral] = (t) =>{
			if (t.unit!=null){
				return new Transaction[] {
					new Transaction(Transaction.Type.ChangeState,State.Moving),
					new Transaction(Transaction.Type.DeselectAll),
					new Transaction(Transaction.Type.ToggleSelect,t.unit)
				}.ToList();
			} else {
				return new Transaction[] {
					new Transaction(Transaction.Type.ChangeState,State.Selecting),
					new Transaction(Transaction.Type.DeselectAll),
					new Transaction(Transaction.Type.ToggleSelect,t.unit)
				}.ToList();
			}
		};

		_mouseUp[State.Moving] = 
		_mouseUp[State.Selecting] = (t) =>{
				return new Transaction[] {new Transaction(Transaction.Type.ChangeState,State.Neutral)
			}.ToList();
		};	

		_mouseMove[State.Moving] = (t) => {
			if (t.selected.Length>0){
				return new Transaction[] {new Transaction(Transaction.Type.MoveObject,t.selected[0],t.position)
				}.ToList();
			} else {
				return new Transaction[] {new Transaction(Transaction.Type.MoveObject,null,t.position)
				}.ToList();
			}
		};

		_transactionMap[Transaction.Type.ChangeState] = t => _state = t.state;
		_transactionMap[Transaction.Type.DeselectAll] = t => _units.ToList().ForEach(u=>u.Selected = false);
		_transactionMap[Transaction.Type.ToggleSelect] = t => {if (t.unit!=null)t.unit.Selected=!t.unit.Selected;};
		_transactionMap[Transaction.Type.MoveObject] = t => {if (t.unit!=null)t.unit.transform.position = t.position;};
			
	}
		
	Vector3 _lastMousePos = new Vector3(0,0,0);
	
	// Update is called once per frame
	void Update () {

		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		Unit hoverUnit=null;
		if (Physics.Raycast(ray,out hit)) {
			hoverUnit = hit.transform.GetComponent<Unit>();
		}

		Vector3 planePoint = ray.origin - (ray.origin.y/ray.direction.y)*ray.direction;

		Unit[] selected = _units.Where(u=>u.Selected).ToArray();

		if (Input.GetMouseButtonDown(0)) {
			_mouseDown[_state](new FuncParams(planePoint
								,hoverUnit
								,_units.Where(u=>u.Selected)
										.ToArray()))
				.ForEach(t=>_transactionMap[t.type](t));
		} 

		if (Input.GetMouseButtonUp(0)) {
			_mouseUp[_state](new FuncParams(planePoint
								,hoverUnit
								,_units.Where(u=>u.Selected)
										.ToArray()))
				.ForEach(t=>_transactionMap[t.type](t));
		} 

		if ((Input.mousePosition-_lastMousePos).magnitude > 0){
			if (_mouseMove.ContainsKey(_state)){
				_mouseMove[_state](new FuncParams(planePoint
									,hoverUnit
									,_units.Where(u=>u.Selected)
											.ToArray()))
					.ForEach(t=>_transactionMap[t.type](t));
			}
		}
	}

	void OnGUI() {
		GUI.TextArea(new Rect(0,0,100,20),_state.ToString());
	}
}
