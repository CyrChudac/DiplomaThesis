using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class ViewconeCreator : ViewConeMaintainer
{
    [SerializeField] private Material viewMaterial;

    private const float defaultAlpha = 0.4f; 
    private static readonly Color defaultOk = Color.green; 
    private static readonly Color defaultMiddle = Color.gray; 
    private static readonly Color defaultBad = Color.red; 

    [SerializeField] private Color okView 
        = new Color(defaultOk.r, defaultOk.g, defaultOk.b, defaultAlpha); 
    [SerializeField] private Color middleLine 
        = new Color(defaultMiddle.r, defaultMiddle.g, defaultMiddle.b, defaultAlpha); 
    [SerializeField] private Color badView 
        = new Color(defaultBad.r, defaultBad.g, defaultBad.b, defaultAlpha);

    [SerializeField] private float middleLineWidth; 
    [SerializeField] private float viewLength;
    [Tooltip("In seconds")]
    [Min(0.0001f)]
    [SerializeField] private float timeUntilFullView = 2f;

    [SerializeField] private GameObject player;
    [Tooltip("The minimal distance from the player when the viewcone is displayed.")]
    [SerializeField] private float displayDistance;

    [Header("Events")]

    [SerializeField] private bool vocalEvents = false;
    [SerializeField] private UnityEvent<GameObject> onSeen; 
    [SerializeField] private UnityEvent<GameObject> onCaught; 
    [SerializeField] private UnityEvent onNotSeenAnymore;
    [SerializeField] private UnityEvent onNoLongerCaught;

    float suspitionRatio = 0;
    bool seen = false;
    bool caught = false;

	protected override void Start() {
		onSeen.AddListener((o) => seen = true);
		onCaught.AddListener((o) => caught = true);
		onNotSeenAnymore.AddListener(() => seen = false);
		onNoLongerCaught.AddListener(() => caught = false);
        if(vocalEvents) {
            onSeen.AddListener((o) => Debug.Log("seen"));
            onCaught.AddListener((o) => Debug.Log("caught"));
            onNotSeenAnymore.AddListener(() => Debug.Log("UN seen"));
            onNoLongerCaught.AddListener(() => Debug.Log("UN caught"));
        }

        var mid = new MiddleViewPart(this);
        ViewconeParts = new IViewconePartSpecifier[] {
            new DangerViewPart(this, mid),
            mid,
            new OkViewPart(this)
        };
        base.Start();
	}

	protected override void Update()
    {
        if(seen || caught) {
            suspitionRatio = Mathf.Min(1, suspitionRatio + Time.deltaTime/timeUntilFullView);
        } else {
            suspitionRatio = Mathf.Max(0, suspitionRatio - Time.deltaTime/timeUntilFullView);
        }
        base.Update();
    }

    bool inMidLine = false;
    bool inDangerPart = false;
    private void OnMiddleLineEnter(GameObject o) {
        if(!(inMidLine || inDangerPart)) {
            onCaught.Invoke(o);
        }
        inMidLine = true;
    }
    private void OnMiddleLineLeft() {
        if(!inDangerPart) {
            onNoLongerCaught.Invoke();
        }
        inMidLine = false;
    }
    private void OnDangerEnter(GameObject o) {
        if(!(inMidLine || inDangerPart)) {
            onCaught.Invoke(o);
        }
        inDangerPart = true;
    }
    private void OnDangerLeft() {
        if(!inMidLine) {
            onNoLongerCaught.Invoke();
        }
        inDangerPart = false;
    }

    abstract class ViewPart : IViewconePartSpecifier {
        protected readonly ViewconeCreator creator;
        public ViewPart(ViewconeCreator creator) {
            this.creator = creator;
        }

        private Material _material = null;
        public Material Material {
            get {
                if(_material == null) {
                    _material = new Material(creator.viewMaterial);
                    _material.SetColor("_Color", Color);
                }
                return _material;
            }
        }

		public virtual bool DisplayThis => Length > 0;

        protected abstract Color Color { get; }

		public abstract string Name { get; }
		public abstract float Length { get; }

        public abstract void OnEnteredBy(GameObject by);

        public abstract void OnLeftBy(GameObject by);
	}

    class OkViewPart : ViewPart {
		public OkViewPart(ViewconeCreator creator) : base(creator) {}

		protected override Color Color => creator.okView;

		public override void OnEnteredBy(GameObject by) {
            creator.onSeen.Invoke(by);
		}

		public override void OnLeftBy(GameObject by) {
            creator.onNotSeenAnymore.Invoke();
		}

		public override string Name => "good view";

		public override float Length => creator.viewLength * (1 - creator.suspitionRatio);
	}

    class MiddleViewPart : ViewPart {
		public MiddleViewPart(ViewconeCreator creator) : base(creator) {}

		protected override Color Color => creator.middleLine;

		public override void OnEnteredBy(GameObject by) {
            creator.OnMiddleLineEnter(by);
		}

		public override void OnLeftBy(GameObject by) {
            creator.OnMiddleLineLeft();
		}

		public override string Name => "mid line view";

		public override bool DisplayThis => base.DisplayThis && creator.middleLineWidth > 0;
		public override float Length => Mathf.Min(
            creator.viewLength * creator.suspitionRatio, 
            creator.middleLineWidth,
            creator.viewLength * (1 - creator.suspitionRatio));
	}
    
    class DangerViewPart : ViewPart {
        readonly MiddleViewPart middleLine;
		public DangerViewPart(ViewconeCreator creator, MiddleViewPart middleLine) : base(creator) {
            this.middleLine = middleLine;
        }

		protected override Color Color => creator.badView;

		public override void OnEnteredBy(GameObject by) {
            creator.OnDangerEnter(by);
		}

		public override void OnLeftBy(GameObject by) {
            creator.OnDangerLeft();
		}

		public override string Name => "danger view";

		public override float Length => Mathf.Max(
            creator.viewLength * creator.suspitionRatio - middleLine.Length,
            0);
	}
}
