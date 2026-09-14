using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace CircuitPuzzle {
public class PuzzlePopup : MonoBehaviour {
    public PuzzleLevel defaultLevel;
    public bool showTestButton=true;
    public bool pauseTime=true;
    [Tooltip("Player movement, mouse look, or PlayerInput components to suspend while open.")]
    public Behaviour[] disableWhileOpen;
    public UnityEvent onOpened=new UnityEvent(), onSolved=new UnityEvent(), onClosed=new UnityEvent();
    public bool IsOpen {get;private set;}
    public static bool AnyOpen {get {return current!=null;}}
    static PuzzlePopup current;
    Canvas canvas; GameObject modal,test; PuzzleBoard board; Text heading,status;
    float oldTime; bool oldVisible; CursorLockMode oldLock; bool[] oldEnabled;
    bool solvedRaised; PuzzleLevel selected;
    void Start(){EnsureUI();}
    // Demo convenience: turn off showTestButton when connecting your real interaction.
    void LateUpdate(){if(showTestButton && !AnyOpen){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}}
    void OnDisable(){Close();if(canvas!=null)canvas.gameObject.SetActive(false);}
    void OnEnable(){if(canvas!=null)canvas.gameObject.SetActive(true);}
    public void Open(){Open(defaultLevel);}
    public void Open(PuzzleLevel level) {
        if(!isActiveAndEnabled || IsOpen || (current!=null && current!=this))return;
        if(level==null){Debug.LogError("Assign a Puzzle Level to PuzzlePopup.",this);return;}
        var problems=level.Problems();if(problems.Count>0){Debug.LogError(string.Join("\n",problems),level);return;}
        EnsureUI();EnsureEventSystem();selected=level;current=this;IsOpen=true;
        oldTime=Time.timeScale;oldVisible=Cursor.visible;oldLock=Cursor.lockState;
        if(pauseTime)Time.timeScale=0;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        oldEnabled=new bool[disableWhileOpen==null?0:disableWhileOpen.Length];
        for(int i=0;i<oldEnabled.Length;i++){var b=disableWhileOpen[i];if(b==null || b==this)continue;oldEnabled[i]=b.enabled;b.enabled=false;}
        modal.SetActive(true);test.SetActive(false);ResetPuzzle();onOpened.Invoke();
    }
    public void Close() {
        if(!IsOpen)return;IsOpen=false;if(current==this)current=null;
        if(pauseTime)Time.timeScale=oldTime;Cursor.lockState=oldLock;Cursor.visible=oldVisible;
        for(int i=0;i<oldEnabled.Length;i++) if(disableWhileOpen[i]!=null && disableWhileOpen[i]!=this)disableWhileOpen[i].enabled=oldEnabled[i];
        if(modal!=null)modal.SetActive(false);if(test!=null)test.SetActive(showTestButton);onClosed.Invoke();
    }
    public void ResetPuzzle(){if(!IsOpen)return;solvedRaised=false;heading.text=selected.title;board.Bind(new PuzzleState(selected));Refresh();}
    void Refresh(){
        if(board.State.Solved){status.text="CIRCUIT COMPLETE — access granted";if(!solvedRaised){solvedRaised=true;onSolved.Invoke();}}
        else status.text="Drag from a solid square. Visit every dot. Connect every start.";
    }
    static void EnsureEventSystem(){
        if(EventSystem.current!=null)return;
        var go=new GameObject("Puzzle EventSystem",typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }
    RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 pos){
        var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.sizeDelta=size;r.anchoredPosition=pos;return r;
    }
    Image Fill(RectTransform r,Color c){var i=r.gameObject.AddComponent<Image>();i.color=c;return i;}
    Text Label(string name,Transform parent,string value,int size,Vector2 dimensions,Vector2 position){
        var r=Rect(name,parent,dimensions,position);var t=r.gameObject.AddComponent<Text>();
#if UNITY_2022_2_OR_NEWER
        t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        t.font=Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        t.text=value;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(.87f,.96f,.94f);t.raycastTarget=false;return t;
    }
    GameObject Button(string name,Transform parent,string caption,Vector2 size,Vector2 pos,UnityAction action){
        var r=Rect(name,parent,size,pos);Fill(r,new Color(.1f,.27f,.27f));var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.onClick.AddListener(action);
        Label("Label",r,caption,20,size,Vector2.zero);return r.gameObject;
    }
    void EnsureUI(){
        if(canvas!=null)return;EnsureEventSystem();
        var root=Rect("Puzzle Canvas",transform,Vector2.zero,Vector2.zero);
        canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;
        var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1000,1000);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        root.gameObject.AddComponent<GraphicRaycaster>();
        test=Button("Test Button",root,"OPEN PUZZLE",new Vector2(210,55),Vector2.zero,Open);
        var tr=test.GetComponent<RectTransform>();tr.anchorMin=tr.anchorMax=new Vector2(.5f,0);tr.anchoredPosition=new Vector2(0,55);test.SetActive(showTestButton);
        var shade=Rect("Modal Blocker",root,Vector2.zero,Vector2.zero);shade.anchorMin=Vector2.zero;shade.anchorMax=Vector2.one;shade.offsetMin=shade.offsetMax=Vector2.zero;
        Fill(shade,new Color(0,.015f,.025f,.9f));modal=shade.gameObject;
        var frame=Rect("Frame",shade,new Vector2(870,940),Vector2.zero);Fill(frame,new Color(.63f,.53f,.23f));
        var panel=Rect("Panel",frame,new Vector2(858,928),Vector2.zero);Fill(panel,new Color(.025f,.065f,.08f));
        heading=Label("Title",panel,"CIRCUIT",30,new Vector2(780,55),new Vector2(0,410));
        var br=Rect("Board",panel,new Vector2(730,680),new Vector2(0,25));br.gameObject.AddComponent<CanvasRenderer>();board=br.gameObject.AddComponent<PuzzleBoard>();board.color=Color.white;board.changed=Refresh;
        status=Label("Status",panel,"",18,new Vector2(810,50),new Vector2(0,-332));
        Label("Legend",panel,"Solid = start   Outline = end   X = blocked   Dot = required",17,new Vector2(820,35),new Vector2(0,-372));
        Button("Reset",panel,"RESET",new Vector2(200,52),new Vector2(-120,-421),ResetPuzzle);
        Button("Close",panel,"CLOSE",new Vector2(200,52),new Vector2(120,-421),Close);
        modal.SetActive(false);
    }
}
}
