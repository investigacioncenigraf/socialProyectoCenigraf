mergeInto(LibraryManager.library, {
  NIWI_CanvasPixelRatio: function () {
    var canvas = Module.canvas;
    var rect = canvas.getBoundingClientRect();
    return rect.width > 0 ? canvas.width / rect.width : (window.devicePixelRatio || 1);
  },
  NIWI_IsMobileDevice: function () {
    var ua = navigator.userAgent || '';
    var ipadDesktop = /Macintosh|MacIntel/.test(ua + ' ' + navigator.platform) && navigator.maxTouchPoints > 1;
    var mobile = /Android|iPhone|iPad|iPod|Mobile|Tablet/i.test(ua) || ipadDesktop;
    if (mobile && typeof Module !== 'undefined' && Module.canvas) Module.canvas.style.touchAction = 'none';
    return mobile ? 1 : 0;
  }
});
