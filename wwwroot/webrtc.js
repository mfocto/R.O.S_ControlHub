const roomId = "cell-1";
const video = document.getElementById("video");

// -----------------
// Error handling
// -----------------
window.onerror = (m, s, l, c, e) =>
    console.error("window.onerror", m, s, l, c, e);

window.onunhandledrejection = (e) =>
    console.error("unhandledrejection", e.reason || e);

// -----------------
// WebRTC config
// -----------------
const iceServers = [{ urls: ["stun:stun.l.google.com:19302"] }];

// -----------------
// SignalR
// -----------------
const conn = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/webrtc")
    .withAutomaticReconnect()
    .build();

// -----------------
// State
// -----------------
let broadcasterId = null;

// 협상 중복 방지
let negotiating = false;

// 현재 PC 및 버퍼들
let pc = null;
let pendingRemoteIce = []; // remoteDescription 설정 전 받은 ICE
let pendingLocalIce = [];  // broadcasterId 확정 전 생성된 local ICE

// -----------------
// Utility: PC 생성 
// -----------------
function createPeerConnection() {
    const pc = new RTCPeerConnection({ iceServers });

    // 수신 전용 트랜시버
    pc.addTransceiver("video", { direction: "recvonly" });

    // 상태 변화 로그 
    pc.oniceconnectionstatechange = () => console.log("ice:", pc.iceConnectionState);
    pc.onconnectionstatechange = () => console.log("pc:", pc.connectionState);

    // 트랙 수신
    pc.ontrack = (e) => {
        console.log("ontrack:", e.track.kind);

        e.track.onunmute = () => console.log("video frames started");

        video.srcObject = new MediaStream([e.track]);
        video.muted = true;
        video.autoplay = true;
        video.playsInline = true;
        video.play().catch(console.warn);
    };

    // 로컬 ICE 발생 -> broadcasterId가 없으면 버퍼링, 있으면 즉시 Relay
    pc.onicecandidate = (e) => {
        if (!e.candidate) return;

        if (!broadcasterId) {
            pendingLocalIce.push(e.candidate);
            return;
        }

        conn.invoke("Relay", roomId, "ice", JSON.stringify(e.candidate), broadcasterId)
            .catch(err => console.warn("relay local ice failed:", err));
    };

    return pc;
}

// -----------------
// Negotiation 
// -----------------
async function negotiateNow() {
    if (!broadcasterId) return;
    if (negotiating) return;

    negotiating = true;

    // 기존 pc 정리 후 새로 생성 (꼬임/지연 원인 제거)
    try {
        if (pc) {
            pc.ontrack = null;
            pc.onicecandidate = null;
            pc.close();
        }
    } catch { /* ignore */ }

    pc = createPeerConnection();

    // offer 생성
    const offer = await pc.createOffer();
    await pc.setLocalDescription(offer);

    // offer 전송
    await conn.invoke("Relay", roomId, "offer", JSON.stringify(offer), broadcasterId);

    // broadcasterId 이전에 생성된 local ICE 후보 flush 
    if (pendingLocalIce.length > 0) {
        const toSend = pendingLocalIce;
        pendingLocalIce = [];

        for (const c of toSend) {
            await conn.invoke("Relay", roomId, "ice", JSON.stringify(c), broadcasterId);
        }
    }

    // negotiating은 answer 수신 이후 해제
}

// -----------------
// Signal handlers
// -----------------
conn.on("BroadcasterOnline", async (id) => {
    console.log("BroadcasterOnline:", id);

    // 자기 자신 id로 broadcaster가 오는 경우 방어 (서버 버그/혼선 대비)
    if (conn.connectionId && id === conn.connectionId) {
        console.warn("Ignoring BroadcasterOnline == my connectionId", id);
        return;
    }

    // 이미 broadcasterId가 있는데 바뀌면 협상 꼬일 수 있으므로 무시(원인 추적용 경고만)
    if (broadcasterId && broadcasterId !== id) {
        console.warn("BroadcasterOnline changed. Ignoring:", id, "current:", broadcasterId);
        return;
    }

    broadcasterId = id;

    // 즉시 협상 시작
    try {
        await negotiateNow();
    } catch (e) {
        negotiating = false;
        console.error("negotiateNow failed:", e);
    }
});

conn.on("Signal", async (type, payload) => {
    const data = JSON.parse(payload);

    if (!pc) {
        // pc가 아직 없으면 (드물지만) remote ICE를 버퍼링
        if (type === "ice") pendingRemoteIce.push(data);
        return;
    }

    if (type === "answer") {
        await pc.setRemoteDescription(data);

        // remoteDescription 설정 후, 대기 중이던 remote ICE 모두 적용
        for (const ice of pendingRemoteIce) {
            await pc.addIceCandidate(ice);
        }
        pendingRemoteIce = [];

        // 협상 완료
        negotiating = false;
        return;
    }

    if (type === "ice") {
        // remoteDescription 없으면 버퍼링
        if (!pc.remoteDescription) {
            pendingRemoteIce.push(data);
            return;
        }
        await pc.addIceCandidate(data);
    }
});

// -----------------
// Optional: Stats (지연/연결 확인용)
// -----------------
setInterval(() => {
    if (!pc) return;
    logCandidatePairTypes(pc).catch(console.warn);
}, 2000);

// -----------------
// Start
// -----------------
async function start() {
    await conn.start();
    await conn.invoke("JoinViewers", roomId);

    // CheckBroadcaster는 제거 권장:
    // - 이전에 broadcasterId를 viewer 자신 id로 덮어쓰는 버그를 만들었음
    // - JoinViewers에서 BroadcasterOnline을 이미 받도록 설계되어 있음
}

async function logCandidatePairTypes(pc) {
    const stats = await pc.getStats();

    // 1) 선택된(=실제로 쓰는) candidate-pair 찾기
    let selectedPair = null;

    // 표준 경로: transport.selectedCandidatePairId
    let selectedPairId = null;
    stats.forEach(r => {
        if (r.type === "transport" && r.selectedCandidatePairId) {
            selectedPairId = r.selectedCandidatePairId;
        }
    });

    if (selectedPairId && stats.get(selectedPairId)) {
        selectedPair = stats.get(selectedPairId);
    }

    // 보조 경로(브라우저/버전에 따라): candidate-pair.selected === true
    if (!selectedPair) {
        stats.forEach(r => {
            if (r.type === "candidate-pair" && (r.selected || r.nominated) && r.state === "succeeded") {
                selectedPair = r;
            }
        });
    }

    if (!selectedPair) {
        console.log("[ICE] selected candidate-pair not found yet");
        return;
    }

    // 2) candidate-pair 자체에 local/remote type이 직접 있는 경우(일부 구현)
    const directLocalType = selectedPair.localCandidateType;
    const directRemoteType = selectedPair.remoteCandidateType;

    // 3) candidateId로 후보 상세를 찾아 type 확인(가장 확실)
    const local = selectedPair.localCandidateId ? stats.get(selectedPair.localCandidateId) : null;
    const remote = selectedPair.remoteCandidateId ? stats.get(selectedPair.remoteCandidateId) : null;

    const localType = directLocalType || local?.candidateType || local?.type;
    const remoteType = directRemoteType || remote?.candidateType || remote?.type;

    console.log("[ICE] selected pair", {
        pairId: selectedPair.id,
        state: selectedPair.state,
        nominated: selectedPair.nominated,
        localCandidateType: localType,
        remoteCandidateType: remoteType,
        localAddress: local?.address,
        localProtocol: local?.protocol,
        localPort: local?.port,
        remoteAddress: remote?.address,
        remoteProtocol: remote?.protocol,
        remotePort: remote?.port,
        currentRoundTripTime: selectedPair.currentRoundTripTime,
        availableOutgoingBitrate: selectedPair.availableOutgoingBitrate,
        bytesSent: selectedPair.bytesSent,
        bytesReceived: selectedPair.bytesReceived
    });
}

start().catch(console.error);

// -----------------
// 제어 버튼 기능
// -----------------

// 제어 명령을 Unity로 전송하는 함수
async function sendControlCommand(command, value) {
    if (!conn || conn.state !== signalR.HubConnectionState.Connected) {
        console.warn("[Control] SignalR 연결이 없습니다");
        updateStatus("연결 없음", false);
        return;
    }

    if (!broadcasterId) {
        console.warn("[Control] Broadcaster가 연결되지 않았습니다");
        updateStatus("Unity 미연결", false);
        return;
    }

    try {
        console.log(`[Control] 명령 전송: ${command}, 값: ${value}`);
        await conn.invoke("SendControlCommand", roomId, command, value);
        updateStatus(`명령 전송: ${value}`, true);
        
        // 상태 메시지를 1초 후 초기화
        setTimeout(() => {
            updateStatus("연결됨", true);
        }, 1000);
    } catch (err) {
        console.error("[Control] 명령 전송 실패:", err);
        updateStatus("전송 실패", false);
    }
}

// 상태 표시 업데이트
function updateStatus(message, isActive) {
    const statusEl = document.getElementById("status");
    if (statusEl) {
        statusEl.textContent = message;
        if (isActive) {
            statusEl.classList.add("active");
        } else {
            statusEl.classList.remove("active");
        }
    }
}

// 페이지 로드 후 버튼 이벤트 리스너 등록
window.addEventListener("DOMContentLoaded", () => {
    // 모든 제어 버튼 가져오기
    const controlButtons = document.querySelectorAll(".control-btn");
    
    controlButtons.forEach(button => {
        button.addEventListener("click", () => {
            const command = button.getAttribute("data-command");
            const value = button.getAttribute("data-value");
            
            if (command && value) {
                sendControlCommand(command, value);
            }
        });
    });
    
    console.log("[Control] 제어 버튼 초기화 완료");
});

// SignalR 연결 상태 변경 시 상태 표시 업데이트
conn.onreconnecting(() => {
    updateStatus("재연결 중...", false);
});

conn.onreconnected(() => {
    updateStatus("재연결됨", true);
});

conn.onclose(() => {
    updateStatus("연결 끊김", false);
});

