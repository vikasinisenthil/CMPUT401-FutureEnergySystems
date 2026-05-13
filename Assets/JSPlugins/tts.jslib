mergeInto(LibraryManager.library, {
    JS_Speak: function (str, volume) {
        var utt = new SpeechSynthesisUtterance(UTF8ToString(str));
        utt.volume = volume;
        window.speechSynthesis.speak(utt);
    },

    JS_Pause: function () {
        window.speechSynthesis.pause();
    },

    JS_Resume: function () {
        window.speechSynthesis.resume();
    },

    JS_Stop: function () {
        window.speechSynthesis.cancel();
    },

    JS_Paused: function () {
        return window.speechSynthesis.paused;
    },

    JS_Pending: function () {
        return window.speechSynthesis.pending;
    },

    JS_Speaking: function () {
        return window.speechSynthesis.speaking;
    },
});