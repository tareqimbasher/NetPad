import Aurelia, {Registration} from 'aurelia';
// import "bootstrap";
// import 'bootstrap/dist/js/bootstrap.bundle';
import './styles/main.scss';
import 'bootstrap-icons/font/bootstrap-icons.scss';

const startup = new URLSearchParams(window.location.search);
const win = startup.get("win");

const app = Aurelia.register(
    Registration.instance(String, "http://localhost:8001"),
);

if (win === "main") {
    const mainWindow = require("./windows/main/main");
    mainWindow.register(app);
} else if (win === "settings") {
    const settingsWindow = require("./windows/settings/main");
    settingsWindow.register(app);
}

app.start();
