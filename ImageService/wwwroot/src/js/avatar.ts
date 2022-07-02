export const avatar = () => {
    const form = document.getElementById('try-avatar') as HTMLFormElement;
    const image = document.getElementById('avatar-image') as HTMLImageElement;

    form.addEventListener('submit', (e) => {
        e.preventDefault();

        const data = Array.from((new FormData(form)).entries())
            .reduce((obj, [key, val]) => Object.assign(obj, { [key]: val }), {}) as Form;

        let url = data.url;
        url += `${data.name}.${data.extension}`;

        let query = [];
        if (data.width) query.push(`width=${data.width}`);
        if (data.height) query.push(`height=${data.height}`);

        if (query.length > 0) url += `?${query.join('&')}`;

        image.src = url;
    })
}

export type Form = {
    name: string;
    extension: string;
    width?: number;
    height?: number;
    url: string;
}