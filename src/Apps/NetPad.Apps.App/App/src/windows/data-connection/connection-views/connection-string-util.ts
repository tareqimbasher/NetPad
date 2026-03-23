interface HasConnectionStringAugment {
    connectionStringAugment?: string | undefined;
}

export function hasTrustServerCertificate(connection: HasConnectionStringAugment): boolean {
    const augment = connection.connectionStringAugment?.toLowerCase().replaceAll(" ", "");
    return !!augment && augment.includes("trustservercertificate=true");
}

export function setConnectionStringKey(connection: HasConnectionStringAugment, key: string, value: string | null) {
    if (!connection.connectionStringAugment) {
        if (value !== null) {
            connection.connectionStringAugment = `${key}=${value};`;
        }
        return;
    }

    const kvs = connection.connectionStringAugment
        .split(";")
        .map(i => i.trim())
        .filter(i => !!i)
        .map(s => s.split("="))
        .filter(s => s.length >= 2);

    let found = false;

    const keyLowered = key.toLowerCase();
    for (const kv of kvs) {
        if (kv[0].toLowerCase() !== keyLowered) {
            continue;
        }

        found = true;

        if (value === null) {
            kv.splice(0);
        } else {
            kv.splice(1);
            kv.push(value);
        }
    }

    if (!found && value !== null) {
        kvs.push([key, value]);
    }

    connection.connectionStringAugment = kvs
        .filter(kv => kv.length > 0)
        .map(kv => `${kv[0]}=${kv.slice(1).join("=")}`)
        .join(";") + ";";
}
