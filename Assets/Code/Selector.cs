using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Selector : MonoBehaviour {

	// App state
	public enum State {
		Neutral,
		Moving,
		Selecting
	}

	// State change transactions
	class Transaction{
		
		public enum Type {
			ChangeState,
			ToggleSelect,
			DeselectAll,
			MoveObject,
			BeginDrag,
			SetSelected
		}

		public Type type;
		public State state;
		public Unit unit;
		public Vector3 position;
		public bool bval;
	}

	// Input handlers
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
	class MouseFuncMap : Dictionary<State,Func<FuncParams,List<Transaction>>>{};
	MouseFuncMap _mouseDown = new MouseFuncMap();
	MouseFuncMap _mouseUp   = new MouseFuncMap();
	MouseFuncMap _mouseMove = new MouseFuncMap();

	// Transaction handlers
	class TransactionMap : Dictionary<Transaction.Type,Action<Transaction>>{};
	TransactionMap _transactionMap = new TransactionMap();
	void ProcessTransaction(Transaction t){
		_transactionMap[t.type](t);
	}

	// State data
	State _state = State.Neutral;
	Unit[] _units;
	Vector3 _lastMousePos = new Vector3(0,0,0);
	Vector3 _mouseDragAnchor = new Vector3(0,0,0);


	// Unity methods
	void Start () {

		GameObject root = GameObject.Find("Units");
		_units = root.GetComponentsInChildren<Unit>();

		Enumerable.Range(0,_units.Length)
			.ToList()
			.ForEach(i=>_units[i].Id = i+1);

		_mouseDown[State.Neutral] = (t) =>{
			if (t.unit!=null){
				return new Transaction[] {
					new Transaction(){type = Transaction.Type.ChangeState,state = State.Moving},
					new Transaction(){type = Transaction.Type.SetSelected,unit = t.unit,bval=true},
					new Transaction(){type = Transaction.Type.BeginDrag,position=t.position},
				}.ToList();
			} else {
				return new Transaction[] {
					new Transaction(){type = Transaction.Type.ChangeState,state = State.Selecting},
					new Transaction(){type = Transaction.Type.DeselectAll}
				}.ToList();
			}
		};

		_mouseUp[State.Moving] = 
		_mouseUp[State.Selecting] = (t) => 
			new Transaction[] {
				new Transaction(){type = Transaction.Type.ChangeState,state = State.Neutral}
			}.ToList();

		_mouseMove[State.Moving] = (t) => 
			t.selected.Select(s=>
				new Transaction(){type = Transaction.Type.MoveObject,unit = s,position = t.position}
			).ToList();

		_transactionMap[Transaction.Type.ChangeState] = t => _state = t.state;
		_transactionMap[Transaction.Type.DeselectAll] = t => _units.ToList().ForEach(u=>u.Selected = false);
		_transactionMap[Transaction.Type.ToggleSelect] = t => t.unit.Selected = !t.unit.Selected;
		_transactionMap[Transaction.Type.SetSelected] = t => t.unit.Selected = t.bval;
		_transactionMap[Transaction.Type.MoveObject] = t => t.unit.transform.position = t.unit.DragOrigin + t.position - _mouseDragAnchor;
		_transactionMap[Transaction.Type.BeginDrag] = t => {_mouseDragAnchor = t.position;
															_units.Where(u=>u.Selected)
																	.ToList()
																	.ForEach(u=>u.BeginDrag());};
	}
		

	void Update () {

		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		Unit hoverUnit=null;
		if (Physics.Raycast(ray,out hit)) {
			hoverUnit = hit.transform.GetComponent<Unit>();
		}

		Vector3 planePoint = ray.origin - (ray.origin.y/ray.direction.y)*ray.direction;

		if (Input.GetMouseButtonDown(0)) {
			_mouseDown[_state](new FuncParams(planePoint
								,hoverUnit
								,_units.Where(u=>u.Selected)
										.ToArray()))
				.ForEach(t=>ProcessTransaction(t));
		} 

		if (Input.GetMouseButtonUp(0)) {
			_mouseUp[_state](new FuncParams(planePoint
								,hoverUnit
								,_units.Where(u=>u.Selected)
										.ToArray()))
				.ForEach(t=>ProcessTransaction(t));
		} 

		if ((Input.mousePosition-_lastMousePos).magnitude > 0){
			if (_mouseMove.ContainsKey(_state)){
				_mouseMove[_state](new FuncParams(planePoint
									,hoverUnit
									,_units.Where(u=>u.Selected)
											.ToArray()))
					.ForEach(t=>ProcessTransaction(t));
			}
		}
	}

	void OnGUI() {
		GUI.TextArea(new Rect(0,0,100,20),_state.ToString());
	}
}
