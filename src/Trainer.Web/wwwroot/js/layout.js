// The page content scrolls inside the frame, not the window, so Blazor's own
// "scroll to top on navigation" does not reach it. MainLayout calls this instead.
window.trainerLayout = {
    scrollTop(element) {
        element?.scrollTo({ top: 0, left: 0, behavior: "instant" });
    },
};
