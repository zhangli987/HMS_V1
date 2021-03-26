using UnityEngine;
using UnityEngine.Events;

namespace Nxr.Internal
{
    [RequireComponent(typeof(Collider))]
    public class NxrInteractive : MonoBehaviour
    { 
        public UnityEvent OnLookExitInvoke;
        public UnityEvent OnLookEnterInvoke;
        public UnityEvent OnTriggerInvoke;

        protected virtual void OnLooked()
        {
            this.OnLookEnterInvoke.Invoke();
        }

        protected virtual void OnUnLooked()
        {
            this.OnLookExitInvoke.Invoke();
        } 

        protected virtual void OnTriggered()
        {
            this.OnTriggerInvoke.Invoke();
        }
         
        public enum GameObjectSightState
        {
            NotLooked,
            Looked,
            Triggered
        }

        public GameObjectSightState _currentState;

        private void Awake()
        {
            this._currentState = GameObjectSightState.NotLooked;
        }

        private void ChangeState(GameObjectSightState stat)
        {
            this._currentState = stat;
        }

        internal void HandleIsLookedAt()
        {
            if (this._currentState == GameObjectSightState.NotLooked)
            {
                this.ChangeState(GameObjectSightState.Looked);
                this.OnLooked();
            }
        }

        internal void OtherIsLooked()
        {
            if (this._currentState != GameObjectSightState.NotLooked)
            {
                this.ChangeState(GameObjectSightState.NotLooked);
                this.OnUnLooked();
            }
        }
         
        private void OnDisable()
        {
            NxrLineOfSight.NonLook -= new NxrLineOfSight.NonLookAction(this.OtherIsLooked);
        }

        private void OnEnable()
        {
            ChangeState(GameObjectSightState.NotLooked);
            NxrLineOfSight.NonLook += new NxrLineOfSight.NonLookAction(this.OtherIsLooked);
        }

        public void OnPointerClicked()
        {
            if (this._currentState == GameObjectSightState.Looked)
            {
                ChangeState(GameObjectSightState.Triggered);
                this.OnTriggered();
            }
        }
    }
}