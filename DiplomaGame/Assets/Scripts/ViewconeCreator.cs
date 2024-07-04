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

    public float viewLength;
    [Tooltip("In seconds")]
    [Min(0.0001f)]
    public float timeUntilFullView = 2f;

    [SerializeField] private GameObject player;
    [Tooltip("The minimal distance from the player when the viewcone is displayed.")]
    [SerializeField] private float displayStartDistance;

    public float suspitionRatio = 0;

	protected override void Start() {

        var mid = new MiddleViewPart(this);
        ViewconeParts = new IViewconePartSpecifier[] {
            new DangerViewPart(this, mid),
            mid,
            new OkViewPart(this)
        };
        base.Start();
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
	}

    class OkViewPart : ViewPart {
		public OkViewPart(ViewconeCreator creator) : base(creator) {}

		protected override Color Color => creator.okView;

		public override string Name => "good view";

		public override float Length => creator.viewLength * (1 - creator.suspitionRatio);
	}

    class MiddleViewPart : ViewPart {
		public MiddleViewPart(ViewconeCreator creator) : base(creator) {}

		protected override Color Color => creator.middleLine;

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

		public override string Name => "danger view";

		public override float Length => Mathf.Max(
            creator.viewLength * creator.suspitionRatio - middleLine.Length,
            0);
	}
}
