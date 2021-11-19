import Aurelia, {Registration} from 'aurelia';
// import "bootstrap";
// import 'bootstrap/dist/js/bootstrap.bundle';
import './styles/main.scss';
import 'bootstrap-icons/font/bootstrap-icons.scss';
import {Index} from './main-window';
import {
    ISession,
    Session,
    IQueryManager,
    QueryManager,
    ISettingsManager,
    SettingsManager,
    ISessionManager, SessionManager
} from "@domain";
import {IBackgroundService, QueryBackgroundService, SessionBackgroundService} from "./main-window/background-services";

Aurelia
    .register(
        Registration.instance(String, "http://localhost:8001"),
        Registration.singleton(ISession, Session),
        Registration.singleton(ISessionManager, SessionManager),
        Registration.singleton(IQueryManager, QueryManager),
        Registration.singleton(ISettingsManager, SettingsManager),
        Registration.singleton(IBackgroundService, SessionBackgroundService),
        Registration.singleton(IBackgroundService, QueryBackgroundService),
    )
    .app({
        host: document.getElementsByTagName("main-window")[0],
        component: Index
    })
    .start();
