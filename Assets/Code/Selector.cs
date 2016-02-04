using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

		_tokenDictionary["find"] = new TokenTag(TokenTag.Type.Verb,"GRASP");
		_tokenDictionary["grasp"] = new TokenTag(TokenTag.Type.Verb,"GRASP");
		_tokenDictionary["find"] = new TokenTag(TokenTag.Type.Verb,"GRASP");
		_tokenDictionary["blue"] = new TokenTag(TokenTag.Type.Adjective,"BLUE");
		_tokenDictionary["red"] = new TokenTag(TokenTag.Type.Adjective,"RED");
		_tokenDictionary["green"] = new TokenTag(TokenTag.Type.Adjective,"GREEN");
		_tokenDictionary["place"] = new TokenTag(TokenTag.Type.Verb,"PUT");
		_tokenDictionary["put"] = new TokenTag(TokenTag.Type.Verb,"PUT");
		_tokenDictionary["it"] = new TokenTag(TokenTag.Type.Pronoun,"IT");
		_tokenDictionary["put"] = new TokenTag(TokenTag.Type.Verb,"PUT");
		_tokenDictionary["cube"] = new TokenTag(TokenTag.Type.Noun,"CUBE");
		_tokenDictionary["sphere"] = new TokenTag(TokenTag.Type.Noun,"SPHERE");
		_tokenDictionary["pyramid"] = new TokenTag(TokenTag.Type.Noun,"PYRAMID");
		_tokenDictionary["box"] = new TokenTag(TokenTag.Type.Noun,"BOX");
		_tokenDictionary["in"] = new TokenTag(TokenTag.Type.Preposition,"INSIDE");
		_tokenDictionary["within"] = new TokenTag(TokenTag.Type.Preposition,"INSIDE");
		_tokenDictionary["inside"] = new TokenTag(TokenTag.Type.Preposition,"INSIDE");
		_tokenDictionary["next_to"] = new TokenTag(TokenTag.Type.Preposition,"NEXT_TO");


	//	testProcessInput("   Find the  blue cube;   and place it in the Red Box. ");
	//	testProcessInput(" Place the large red cube next to the small blue sphere");
		testProcessInput(" Place the large red cube next to the small blue sphere next to the green pyramid");
	}

	class TokenTag
	{
		public enum Type {
			Verb,
			Noun,
			Adjective,
			Pronoun,
			Preposition
		};

		public Type type;
		public string token;

		public TokenTag[] children = new TokenTag[0];
		public TokenTag parent = null;

		public TokenTag(Type t,string s){
			type = t;
			token = s;
		}
	};
		
	class TokenDictionary : Dictionary<string,TokenTag> {};

	private TokenDictionary _tokenDictionary = new TokenDictionary();

	bool BuildTokenNodes(TokenTag.Type[] pattern,int parent,TokenTag[] tokens)
	{
		bool ret = false;
		for (int i = pattern.Length-1;i<tokens.Length;++i)
		{
			bool match = true;
			int baseIndex = i-pattern.Length+1;
			for (int j=0;j<pattern.Length;++j)
			{
				if (pattern[j] != tokens[baseIndex+j].type || tokens[baseIndex+j].parent != null)
				{
					match = false;
				}
			}
			if (match)
			{
				ret = true;
				int parentIndex = baseIndex+parent;
				for (int j=0;j<pattern.Length;++j)
				{
					if (j==parent) continue;
					int childIndex = baseIndex+j;
					List<TokenTag> ltt = tokens[parentIndex].children.ToList();
					ltt.Add(tokens[childIndex]);
					tokens[parentIndex].children = ltt.ToArray();
					tokens[childIndex].parent = tokens[parentIndex];
				}
			}
		}
		return ret;
	}

	string PrintTree(TokenTag[] tags)
	{
		string ret = "";
		foreach(TokenTag t in tags){
			ret += PrintTreeNode(t);
		}
		return ret;
	}

	string PrintTreeNode(TokenTag tag)
	{
		var ret = "(" + tag.token;
		foreach(TokenTag t in tag.children){
			ret = ret + PrintTreeNode(t);
		}
		ret += ")";
		return ret;
	}

	void testProcessInput(string s)
	{
		string tl = s.ToLower();
		string tlf = Regex.Replace(tl,@"[?.,;:!]","");
		string tr = tlf.Trim();
		string trs = Regex.Replace(tr,@"\s\s+"," ");
		trs = Regex.Replace(trs,@"next to","next_to");
		string[] toks = trs.Split();

		TokenTag[] tags = toks.Where(t => _tokenDictionary.ContainsKey(t))
								.Select(t=>_tokenDictionary[t])
								.ToArray();

		TokenTag.Type[] adjectiveNoun = {TokenTag.Type.Adjective,TokenTag.Type.Noun};
		TokenTag.Type[] prepositionNoun = {TokenTag.Type.Preposition,TokenTag.Type.Noun};
		TokenTag.Type[] nounPreposition = {TokenTag.Type.Noun,TokenTag.Type.Preposition};
		TokenTag.Type[] pronounPreposition = {TokenTag.Type.Pronoun,TokenTag.Type.Preposition};
		TokenTag.Type[] verbNoun = {TokenTag.Type.Verb,TokenTag.Type.Noun};
		TokenTag.Type[] verbPronoun = {TokenTag.Type.Verb,TokenTag.Type.Pronoun};

		string command = "";


		while (BuildTokenNodes(adjectiveNoun,1,tags)) { tags = tags.Where(t=>t.parent==null).ToArray(); command = PrintTree(tags);}
		while (BuildTokenNodes(prepositionNoun,0,tags)) {tags = tags.Where(t=>t.parent==null).ToArray(); command = PrintTree(tags);}
		while (BuildTokenNodes(nounPreposition,0,tags)){tags = tags.Where(t=>t.parent==null).ToArray(); command = PrintTree(tags);}
		while (BuildTokenNodes(pronounPreposition,0,tags)){tags = tags.Where(t=>t.parent==null).ToArray(); command = PrintTree(tags);}
		while (BuildTokenNodes(verbNoun,0,tags)){tags = tags.Where(t=>t.parent==null).ToArray(); command = PrintTree(tags);}
		while (BuildTokenNodes(verbPronoun,0,tags)){tags = tags.Where(t=>t.parent==null).ToArray(); command = PrintTree(tags);}



		PrintTree(tags);

		Debug.Log("derp");
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

	public void OnTextEnter(string text){
		UnityEngine.UI.InputField inputField = Canvas.FindObjectOfType<UnityEngine.UI.InputField>();
		inputField.text = "";
	}
}
