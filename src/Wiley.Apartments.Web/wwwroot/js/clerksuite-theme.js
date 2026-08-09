window.ClerkSuiteTheme = {
  storageKey: "clerksuite-theme",
  lightHref: "css/themes/fluent2.min.css",
  darkHref: "css/themes/fluent2-dark.min.css",

  apply(isDark) {
    const themeLink = document.getElementById("syncfusion-theme");
    if (themeLink) {
      themeLink.href = isDark ? this.darkHref : this.lightHref;
    }

    const mode = isDark ? "dark" : "light";
    document.documentElement.setAttribute("data-theme", mode);
    document.documentElement.setAttribute("data-bs-theme", mode);
  },

  setDarkMode(isDark) {
    localStorage.setItem(this.storageKey, isDark ? "dark" : "light");
    this.apply(isDark);
  },

  isDarkMode() {
    return localStorage.getItem(this.storageKey) === "dark";
  },

  init() {
    this.apply(this.isDarkMode());
  },
};
