// MediaRecorder JS interop for Blazor RecordSession page
// Supports AC-1.2.1 (start), AC-1.2.2 (stop/save), AC-1.2.3 (permission denied)

window.mediaRecorderInterop = (() => {
    let _stream = null;
    let _recorder = null;
    let _chunks = [];
    let _dotNetRef = null;

    async function startRecording(dotNetRef, videoElementId) {
        _dotNetRef = dotNetRef;
        _chunks = [];
        try {
            _stream = await navigator.mediaDevices.getUserMedia({ audio: true, video: true });
        } catch (err) {
            // AC-1.2.3 — permission denied or device not available
            const msg = err.name === 'NotAllowedError' || err.name === 'PermissionDeniedError'
                ? 'permission-denied'
                : `error:${err.message}`;
            await dotNetRef.invokeMethodAsync('OnRecordingError', msg);
            return;
        }

        // Attach live preview to <video> element if provided
        if (videoElementId) {
            const vid = document.getElementById(videoElementId);
            if (vid) {
                vid.srcObject = _stream;
                vid.muted = true;
                vid.play().catch(() => {});
            }
        }

        // Pick best supported MIME type
        const mimeType = ['video/webm;codecs=vp9,opus', 'video/webm', 'audio/webm', '']
            .find(m => m === '' || MediaRecorder.isTypeSupported(m));

        _recorder = new MediaRecorder(_stream, mimeType ? { mimeType } : {});
        _recorder.ondataavailable = e => { if (e.data && e.data.size > 0) _chunks.push(e.data); };
        _recorder.onstop = async () => {
            const blob = new Blob(_chunks, { type: _recorder.mimeType || 'video/webm' });
            _chunks = [];
            stopStream();

            // Convert to base64 so we can pass back to Blazor
            const reader = new FileReader();
            reader.onloadend = async () => {
                // reader.result is "data:<mime>;base64,<data>"
                const base64 = reader.result.split(',')[1];
                await _dotNetRef.invokeMethodAsync('OnRecordingStopped', base64, blob.type, blob.size);
            };
            reader.readAsDataURL(blob);
        };
        _recorder.start(1000); // collect 1-second chunks
        await dotNetRef.invokeMethodAsync('OnRecordingStarted');
    }

    function stopRecording() {
        if (_recorder && _recorder.state !== 'inactive') {
            _recorder.stop();
        }
    }

    function stopStream() {
        if (_stream) {
            _stream.getTracks().forEach(t => t.stop());
            _stream = null;
        }
    }

    function cancelRecording() {
        stopStream();
        if (_recorder && _recorder.state !== 'inactive') {
            _recorder.stop();
        }
        _chunks = [];
        _recorder = null;
    }

    function isSupported() {
        return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia && window.MediaRecorder);
    }

    return { startRecording, stopRecording, cancelRecording, isSupported };
})();
