# Graph Report - Scripts  (2026-10-05)

## Corpus Check
- Corpus is ~3,403 words - fits in a single context window. You may not need a graph.

## Summary
- 257 nodes · 394 edges · 14 communities (13 shown, 1 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 4 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Community 0
- Community 1
- Community 2
- Community 3
- Community 4
- Community 5
- Community 6
- Community 7
- Community 8
- Community 9
- Community 10
- Community 11
- Community 12
- Community 13

## God Nodes (most connected - your core abstractions)
1. `UIManager` - 32 edges
2. `KeyboardManager` - 24 edges
3. `ConnectionManager` - 14 edges
4. `VRNetworkPlayer` - 13 edges
5. `HighFiveCounter` - 11 edges
6. `NetworkUIPermission` - 11 edges
7. `NetworkSetRandomColor` - 11 edges
8. `PlayerRole` - 10 edges
9. `PresentationState` - 10 edges
10. `ProximityVoiceManager` - 10 edges

## Surprising Connections (you probably didn't know these)
- `UIManager` --references--> `ConnectionManager`  [EXTRACTED]
  Network UI/UIManager.cs → Connection  Session/ConnectionManager.cs
- `ProximityVoiceManager` --references--> `ConnectionManager`  [EXTRACTED]
  Voice/ProximityVoiceManager.cs → Connection  Session/ConnectionManager.cs
- `UIManager` --references--> `PlayerRole`  [EXTRACTED]
  Network UI/UIManager.cs → Network Player/PlayerRole.cs
- `NetworkUI` --references--> `ConnectionManager`  [EXTRACTED]
  Network UI/NetworkUI.cs → Connection  Session/ConnectionManager.cs
- `NetworkUIPermission` --references--> `VRNetworkPlayer`  [EXTRACTED]
  Network UI/NetworkUIPermission.cs → Network Player/VRNetworkPlayer.cs

## Import Cycles
- None detected.

## Communities (14 total, 1 thin omitted)

### Community 0 - "Community 0"
Cohesion: 0.09
Nodes (17): system, system_collections, system_collections_generic, system_threading_tasks, tmpro, unity_netcode, unity_services_authentication, unity_services_core (+9 more)

### Community 1 - "Community 1"
Cohesion: 0.09
Nodes (19): IXRSelectFilter, IXRSelectInteractable, IXRSelectInteractor, PlayerRole, Guest, None, Presenter, List (+11 more)

### Community 2 - "Community 2"
Cohesion: 0.13
Nodes (7): Button, ConnectionState, GameObject, TextMeshProUGUI, TMP_InputField, UIManager, SelectedRole

### Community 3 - "Community 3"
Cohesion: 0.13
Nodes (8): GameObject, TMP_InputField, UnityEvent, KeyboardManager, ActiveInputField, Instance, IsCapsLock, IsKeyboardVisible

### Community 4 - "Community 4"
Cohesion: 0.11
Nodes (13): BaseEventData, ISelectHandler, TMP_InputField, KeyboardInputField, MonoBehaviour, BodyFollow, Transform, Color (+5 more)

### Community 5 - "Community 5"
Cohesion: 0.11
Nodes (14): ConnectionManager, ConnectionState, Connecting, Disconnected, Disconnecting, Failed, Success, UnityEvent (+6 more)

### Community 6 - "Community 6"
Cohesion: 0.12
Nodes (12): AnimationInput, Animator, List, NetworkAnimationInput, NetworkBehaviour, NetworkGrababble, SelectEnterEventArgs, unityengine_xr_interaction_toolkit (+4 more)

### Community 7 - "Community 7"
Cohesion: 0.17
Nodes (7): NetworkVariable, Rpc, PresentationState, PageCount, GameObject, List, PresentationUI

### Community 8 - "Community 8"
Cohesion: 0.17
Nodes (10): Button, KeyboardKey, KeyType, Backspace, CapsLock, Character, Clear, Enter (+2 more)

### Community 10 - "Community 10"
Cohesion: 0.20
Nodes (6): UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets, unityengine_xr_interaction_toolkit_inputs_readers, Vector2, Transform, NetworkControllerAnimator, XRInputValueReader

### Community 11 - "Community 11"
Cohesion: 0.24
Nodes (4): NetworkVariable, TextMeshProUGUI, HighFiveCounter, Instance

### Community 12 - "Community 12"
Cohesion: 0.33
Nodes (4): Color, NetworkVariable, Renderer, NetworkSetRandomColor

### Community 13 - "Community 13"
Cohesion: 0.31
Nodes (5): Collider, GameObject, Rpc, HighFiveDetector, Vector3

## Knowledge Gaps
- **25 isolated node(s):** `Connecting`, `Failed`, `Success`, `Disconnecting`, `Disconnected` (+20 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 112 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **1 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `UIManager` connect `Community 2` to `Community 0`, `Community 1`, `Community 4`, `Community 5`?**
  _High betweenness centrality (0.234) - this node is a cross-community bridge._
- **Why does `KeyboardManager` connect `Community 3` to `Community 0`, `Community 4`?**
  _High betweenness centrality (0.166) - this node is a cross-community bridge._
- **Why does `ConnectionManager` connect `Community 5` to `Community 0`, `Community 9`, `Community 2`, `Community 4`?**
  _High betweenness centrality (0.136) - this node is a cross-community bridge._
- **What connects `Connecting`, `Failed`, `Success` to the rest of the system?**
  _25 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Community 0` be split into smaller, more focused modules?**
  _Cohesion score 0.09365079365079365 - nodes in this community are weakly interconnected._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.09032258064516129 - nodes in this community are weakly interconnected._
- **Should `Community 2` be split into smaller, more focused modules?**
  _Cohesion score 0.12561576354679804 - nodes in this community are weakly interconnected._