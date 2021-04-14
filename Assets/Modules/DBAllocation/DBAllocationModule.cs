using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DBAllocationModule : MonoBehaviour {
	private const int LINES_COUNT = 14;
	private const int MAX_COMMAND_LENGTH = 27;
	private const string INPUT_PREFIX = "<color=#2f2> > ";
	private const string NUMBER_COLOR = "#44f";

	public TextMesh TextField;

	private class Server {
		public readonly int id;
		public readonly int size;
		Vector2Int? allocation = null;
		public Server(int id, int size) {
			this.id = id;
			this.size = size;
		}
	}

	private bool typing = false;
	private bool selected = false;
	private bool shouldUpdateText = true;
	private int linePointer = LINES_COUNT - 1;
	private string command = "";
	private Server[] servers;
	private string[] text = new string[LINES_COUNT];
	private HashSet<int> serverIds = new HashSet<int>();

	private void Start() {
		KMBombModule module = GetComponent<KMBombModule>();
		module.OnActivate += Activate;
		KMSelectable selfSelectable = GetComponent<KMSelectable>();
		selfSelectable.OnFocus += () => selected = true;
		selfSelectable.OnDefocus += () => selected = false;
	}

	private void Activate() {
		for (int i = 0; i < 10; i++) serverIds.Add(Random.Range(0, 100));
		servers = new Server[serverIds.Count];
		int j = 0;
		foreach (int serverId in serverIds) servers[j++] = new Server(serverId, Random.Range(1, 11));
		typing = true;
		UpdateText();
	}

	private void Update() {
		if (shouldUpdateText) UpdateText();
	}

	private void OnGUI() {
		if (!selected) return;
		Event e = Event.current;
		if (e.type != EventType.KeyDown) return;
		if (ProcessKey(e.keyCode)) shouldUpdateText = true;
	}

	private bool ProcessKey(KeyCode key) {
		if (!typing) return false;
		if (key == KeyCode.Return || key == KeyCode.KeypadEnter) {
			UpdateText();
			if (command.Length < MAX_COMMAND_LENGTH) {
				text[linePointer] = text[linePointer].Remove(text[linePointer].Length - 9, 1);
			}
			linePointer = (linePointer + 1) % LINES_COUNT;
			ProcessCommand();
			command = "";
			return true;
		}
		if (key == KeyCode.Backspace && command.Length > 0) {
			command = command.Remove(command.Length - 1);
			return true;
		}
		if (command.Length >= MAX_COMMAND_LENGTH) return false;
		if (key == KeyCode.Space) {
			command += " ";
			return true;
		}
		if (key >= KeyCode.A && key <= KeyCode.Z) {
			string add = key.ToString();
			if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) add = add.ToLower();
			command += add;
			return true;
		}
		if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9) key = KeyCode.Alpha0 + (key - KeyCode.Keypad0);
		if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9) {
			command += key - KeyCode.Alpha0;
			return true;
		}
		return false;
	}

	private void ProcessCommand() {
		command = command.Trim().ToLower();
		if (command == "") return;
		if (command == "clear") {
			for (int i = 0; i < LINES_COUNT; i++) text[i] = "";
			return;
		}
		if (command == "status") {
			WriteLine(string.Format("Allocated servers: <color={0}>0</color>/<color={0}>7</color>", NUMBER_COLOR));
			WriteLine(string.Format("Damaged nodes: <color={0}>10</color>", NUMBER_COLOR));
			WriteLine(string.Format("Recovered nodes: <color={0}>0</color>", NUMBER_COLOR));
			return;
		}
		if (command == "serverlist") {
			foreach (Server server in servers) {
				Debug.Log(server);
				WriteLine(string.Format("Server <color=yellow>{0}</color>: <color={1}>{2}</color> TB",
					("#" + server.id).PadLeft(3), NUMBER_COLOR, server.size.ToString().PadLeft(2)));
			}
			return;
		}
		WriteLine("<color=red>ERROR</color>: Unknown command");
	}

	private void WriteLine(string str) {
		text[linePointer++] = str;
		if (linePointer >= LINES_COUNT) linePointer = 0;
	}

	private void UpdateText() {
		if (typing) {
			text[linePointer] = new string[] {
				INPUT_PREFIX,
				command,
				command.Length < MAX_COMMAND_LENGTH ? "_" : "",
				"</color>",
			}.Join("");
		}
		TextField.text = Enumerable.Range(linePointer + 1, LINES_COUNT).Select((i) => (
			text[i % LINES_COUNT]
		)).Join("\n");
		shouldUpdateText = false;
	}
}
