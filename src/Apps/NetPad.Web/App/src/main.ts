import Aurelia, {IHttpClient, Registration} from 'aurelia';
import "bootstrap";
import './styles/main.scss';
import {Index} from './main-window';
import {Settings} from "@domain";
import {Mapper} from "@common";

Aurelia
    .register(Registration.cachedCallback(Settings, async (container) =>
    {
        const httpClient = container.get<IHttpClient>(IHttpClient);
        const response = await httpClient.get("settings");
        return Mapper.toModel(Settings, await response.json());
    }))
    .app({
        host: document.getElementsByTagName("main-window")[0],
        component: Index
    })
    .start();
