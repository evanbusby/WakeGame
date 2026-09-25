// Only ever compiled into WebGL builds (the .jslib extension is WebGL-only
// by Unity's own convention). Exposes the browser's own mobile/desktop
// detection to C#, since Unity's Input.touchSupported is unreliable in a
// browser - many desktop browsers report touch support even with no
// touchscreen (Windows precision touchpads, devtools touch emulation, etc).
mergeInto(LibraryManager.library, {
  WG_IsMobileBrowser: function () {
    var ua = (navigator.userAgent || navigator.vendor || window.opera || '');
    var isMobile = /android|iphone|ipad|ipod|iemobile|blackberry|bada|windows phone|mobile/i.test(ua);
    return isMobile ? 1 : 0;
  }
});
