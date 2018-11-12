// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

var startCountdown = function(elem, countDownDate) {
    var timer = setInterval(function() {
        var now = new Date().getTime();
    
        var distance = countDownDate.getTime() - now;
    
        if (distance < 0) {
            clearInterval(timer);
            return;
        }

        var days = Math.floor(distance / (1000 * 60 * 60 * 24));
        var hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        var minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        var seconds = Math.floor((distance % (1000 * 60)) / 1000);
    
        $(elem).html(`
            <span class="text-center font-s-2-5-rem">
                <i class="fas fa-hourglass pr-2"></i>
                <b>${days}</b>d <b>${hours}</b>h <b>${minutes}</b>m <b>${seconds}</b>s
            </span>`);
    }, 1000);
};

var toggleLoader = function() {
    $("#loader-container").toggleClass("d-flex").toggleClass("d-none");
}
