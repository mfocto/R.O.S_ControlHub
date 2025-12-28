const roomId = 'cell-1';
const video = document.getElementById('video');

// -----------
// Error handling 
// -----------
window.onerror = (m, s, l, c, e) =>
    console.error("window.onerror", m, s, l, c, e);

window.onunhandledrejection = (e) =>
    console.error("unhandledrejection", e.reason || e);

// -----------
// WebRTC config
// -----------
const iceServers = [
    { urls: ["stun:stun.l.google.com:19302"] }
];

let broadcasterId = null;
let broadcasterReady = false;
let negotiating = false;

// -----------
// SignalR
// -----------
const conn = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/webrtc')
    .withAutomaticReconnect()
    .build();

// -----------
// PeerConnection
// -----------
const pc = new RTCPeerConnection({ iceServers });
pc.addTransceiver("video", { direction: "recvonly" });

// 상태 변화 로그 
pc.oniceconnectionstatechange = () =>
    console.log("ice:", pc.iceConnectionState);

pc.onconnectionstatechange = () =>
    console.log("pc:", pc.connectionState);

// -----------
// Track handling 
// -----------
pc.ontrack = e => {
    console.log("ontrack:", e.track.kind);

    e.track.onunmute = () =>
        console.log("video frames started");

    video.srcObject = new MediaStream([e.track]);
    video.muted = true;
    video.autoplay = true;
    video.playsInline = true;
    video.play().catch(console.warn);
};

// -----------
// SignalR events
// -----------

async function tryNegotiate() {
    if (!broadcasterId) return;
    if (negotiating) return;
    negotiating = true;

    const offer = await pc.createOffer();
    await pc.setLocalDescription(offer);

    await conn.invoke("Relay", roomId, "offer", JSON.stringify(offer), broadcasterId);
}

conn.on('BroadcasterOnline', async id => {
    broadcasterId = id;
    console.log("BroadcasterOnline:", id);

    // 새 창 열어도 즉시 협상 시작
    await tryNegotiate();
});

// conn.on('BroadcasterReady', async () => {
//     broadcasterReady = true;
//     await tryNegotiate();
// });

conn.on('Signal', async (type, payload) => {
    const data = JSON.parse(payload);

    if (type === 'answer') {
        await pc.setRemoteDescription(data);
    }

    if (type === 'ice') {
        await pc.addIceCandidate(data);
    }
});

// -----------
// ICE candidate relay
// -----------
pc.onicecandidate = e => {
    if (e.candidate && broadcasterId) {
        conn.invoke(
            'Relay',
            roomId,
            'ice',
            JSON.stringify(e.candidate),
            broadcasterId
        );
    }
};

// -----------
// Stats 
// -----------
setInterval(async () => {
    const stats = await pc.getStats();

    stats.forEach(r => {
        const media = r.kind || r.mediaType;

        if (r.type === "inbound-rtp" && media === "video") {
            console.log("[IN video]", {
                bytes: r.bytesReceived,
                frames: r.framesDecoded,
                packetsLost: r.packetsLost
            });
        }
    });
}, 2000);

// -----------
// Start
// -----------
async function start() {
    await conn.start();
    await conn.invoke('JoinViewers', roomId);
    await conn.invoke("CheckBroadcaster", roomId);
}

start();
