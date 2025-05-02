using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallSyn : MonoBehaviourPun, IPunObservable
{
    // 네트워크 관련 변수
    Vector3 networkPos;
    Vector3 networkVel;
    
    // 부드러운 보간을 위한 변수
    Vector3 targetPos;
    Vector3 targetVel;
    Vector3 currentVel;
    float smoothTime = 0.1f; // 부드러운 움직임을 위한 초기값
    
    // 지연 관련 변수
    float lastLag = 0;
    
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        networkPos = transform.position;
        targetPos = transform.position;
        networkVel = Vector3.zero;
        targetVel = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            // 네트워크 지연에 따라 보간 시간 동적 조정
            smoothTime = Mathf.Clamp(lastLag * 0.5f, 0.05f, 0.15f);
            
            // SmoothDamp를 사용한 부드러운 보간
            Vector3 nextPos = Vector3.SmoothDamp(rb.position, targetPos, ref currentVel, smoothTime);
            rb.MovePosition(nextPos);
            
            // 속도 적용 (선택적)
            if (targetVel.magnitude > 0.1f)
            {
                rb.velocity = Vector3.Lerp(rb.velocity, targetVel, Time.fixedDeltaTime * 10f);
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 소유자는 위치와 속도 정보를 전송
            stream.SendNext(transform.position);
            stream.SendNext(rb.velocity);
        }
        else
        {
            // 수신된 위치와 속도 업데이트
            networkPos = (Vector3)stream.ReceiveNext();
            networkVel = (Vector3)stream.ReceiveNext();

            // 지연 계산 및 위치 예측
            lastLag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            targetPos = networkPos + networkVel * lastLag;
            targetVel = networkVel;
            
            // 처음 받았을 때는 즉시 적용하여 큰 점프 방지
            if (currentVel.magnitude < 0.1f)
            {
                rb.position = targetPos;
            }
        }
    }
}