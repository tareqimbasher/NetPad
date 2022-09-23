import Split from "split.js";

export class Sidebar {
    public async attached() {
        Split(["#connection-list", "#script-list"], {
            gutterSize: 6,
            direction: 'vertical',
            sizes: [50, 50],
            minSize: [100, 100],
        });
    }
}

