import React, { Component, PureComponent } from 'react';
import { Helmet } from "react-helmet";
import Loader from "@skbkontur/react-ui/Loader"
import * as PropTypes from "prop-types";
import { saveAs } from "file-saver";
import { connect } from "react-redux"
import api from "../../api"
import {getQueryStringParameter} from "../../utils";
import {Page} from "../../pages";


function getUrlParts(url) {
    let a = document.createElement('a');
    a.href = url;

    return {
        href: a.href,
        host: a.host,
        hostname: a.hostname,
        port: a.port,
        pathname: a.pathname,
        protocol: a.protocol,
        hash: a.hash,
        search: a.search
    };
}

function safeEval(code) {
    try {
// eslint-disable-next-line
        eval(code)
    } catch (e) {
        console.error(e);
    }
}

let decodeHtmlEntities = (function () {
    // this prevents any overhead from creating the object each time
    let element = document.createElement('div');

    function decodeEntities(str) {
        if (str && typeof str === 'string') {
            // strip script/html tags
            str = str.replace(/<script[^>]*>([\S\s]*?)<\/script>/gmi, '');
            str = str.replace(/<\/?\w(?:[^"'>]|"[^"]*"|'[^']*')*>/gmi, '');
            element.innerHTML = str;
            str = element.textContent;
            element.textContent = '';
        }

        return str;
    }

    return decodeEntities;
})();

class DownloadedHtmlContent extends Component {
    BASE_URL = '';

    constructor(props) {
        super(props);

        this.state = {
            loading: true,
            body: '',
			bodyClassName: '',
			containerType: '',
            meta: {},
            links: []
        };
    }

    componentDidMount() {
        this.fetchContentFromServer(this.props.url);
    }

    componentWillReceiveProps(nextProps) {
        if (this.props.url !== nextProps.url || this.props.account.isAuthenticated !== nextProps.account.isAuthenticated)
            this.fetchContentFromServer(nextProps.url);
    }

    static removeBootstrapModalBackdrop() {
        let body = document.getElementsByTagName('body')[0];
        body.classList.remove('modal-open');
        let backdrop = body.getElementsByClassName('modal-backdrop')[0];
        if (backdrop)
            backdrop.remove();
    }

    static removeStickyHeaderAndColumn() {
        Array.from(document.getElementsByClassName('sticky-header')).forEach(r => r.remove());
        Array.from(document.getElementsByClassName('sticky-column')).forEach(r => r.remove());
    }

    static getCurrentBodyContent() {
        let body = document.getElementsByTagName('body')[0];
        return body.innerHTML;
    }

    fetchContentFromServer(url) {
        const self = this;

        let courseId = this._getCourseIdFromUrl();
        this.props.enterToCourse(courseId);

        fetch(this.BASE_URL + url, {credentials: 'include'})
            .then(response => {
                if (response.redirected) {
                    /* If it was a redirect from external login callback, then update user information */
                    const oldUrlPathname = getUrlParts(url).pathname;
                    if (oldUrlPathname.startsWith("/Login/ExternalLoginCallback"))
                        this.props.updateUserInformation();

                    let newUrl = getUrlParts(response.url);
                    this.context.router.history.replace(newUrl.pathname + newUrl.search);
                    return Promise.resolve(undefined);
                }
                /* Process attaches: download them and return url back */
                if (response.headers.has('Content-Disposition')) {
                    let contentDisposition = response.headers.get('Content-Disposition');
                    if (contentDisposition.indexOf('attachment') !== -1) {
                        const filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
                        let matches = filenameRegex.exec(contentDisposition);
                        if (matches != null && matches[1]) {
                            let filename = matches[1].replace(/['"]/g, '');
                            response.blob().then(blob => this.downloadFile(blob, filename));
                            return Promise.resolve(undefined);
                        }
                    }
                }
                /* Process content files: also download them and return url back */
                if (url.startsWith('/Content/') || url.startsWith('/Certificates/')) {
                    response.blob().then(blob => this.downloadFile(blob, url));
                    return Promise.resolve(undefined);
                }
                this.setState(s => {
                    s.loading = true;
                    return s;
                });
                return response.text();
            })
            .then(data => {
                if (data === undefined) {
                    return;
                }

                this.processNewHtmlContent(url, data);
            }).catch(function(error) {
                console.error(error);
                /* Retry after timeout */
                setTimeout(() => self.fetchContentFromServer(url), 5000);
            });
    }

    processNewHtmlContent(url, data) {
        /* In case if we haven't do it yet, get courseId from URL now */
        let courseId = this._getCourseIdFromUrl();
        this.props.enterToCourse(courseId);

        let el = document.createElement('html');
        el.innerHTML = data;
        let head = el.getElementsByTagName('head')[0];
        let body = el.getElementsByTagName('body')[0];
        let containerType = this._getContainerType(url, body);

        let links = Array.from(head.getElementsByTagName('link'));
        let titles = head.getElementsByTagName('title');

        this.setState({
            loading: false,
            body: body.innerHTML,
			bodyClassName: body.className,
			containerType: containerType,
            links: links
        });

        DownloadedHtmlContent.removeStickyHeaderAndColumn();

        /* Run scripts */
        (window.documentReadyFunctions || []).forEach(f => f());

        window.meta = undefined;
        let allScriptTags = Array.from(body.getElementsByTagName('script'));
        /* Eval embedded scripts */
        let embeddedScripts = allScriptTags.filter(s => !s.src).map(s => s.innerHTML);
        embeddedScripts.forEach(safeEval);
        /* Eval remote scripts */
        allScriptTags.filter(s => s.src).map(s => s.src).forEach(url => {
            fetch(url).then(r => r.text()).then(safeEval);
        });

        /* Scroll to top */
        window.scrollTo(0, 0);

        this.loadContentByClass();
        this.setPostFormSubmitHandler();

        let meta = window.meta || {
            title: titles ? titles[0].innerText : 'Ulearn',
            description: 'Интерактивные учебные онлайн-курсы по программированию',
            keywords: '',
            imageUrl: '',
        };
        this.setState(s => {
            s.loading = false;
            s.meta = meta;
            return s;
        });

        this.lastRenderedUrl = url;
        DownloadedHtmlContent.removeBootstrapModalBackdrop();
    }

	_getContainerType(url, body) {
		if (body.getElementsByClassName('wide-container').length > 0 || body.getElementsByClassName('main-page').length > 0)
			return 'wide';
		if (body.getElementsByClassName('slide-container').length > 0)
			return 'slide';
		if (body.getElementsByClassName('container').length > 0)
			return 'common';
		return ''
	}

    _getCourseIdFromUrl() {
        /* 1. Extract courseId from urls like /Course/<courseId/... */
        const pathname = window.location.pathname;
        if (pathname.startsWith('/Course/')) {
            const regex = new RegExp('/Course/([^/]+)(/|$)');
            const results = regex.exec(pathname);
            return results[1].toLowerCase();
        }

        /* 2. Extract courseId from query string: ?courseId=BasicProgramming */
        const courseIdFromQueryString = getQueryStringParameter("courseId");
        if (courseIdFromQueryString)
            return courseIdFromQueryString.toLowerCase();

        /* 3. Return undefined if courseId is not found */
        return undefined;
    }

    downloadFile(blob, filename) {
        saveAs(blob, filename, false);
        if (this.lastRenderedUrl)
            window.history.replaceState({}, '', this.lastRenderedUrl);
    }

    render() {
        if (this.state.loading) {
            return (
                <Loader type="big" active>
                    { this.getContent() }
                </Loader>
            )
        }

        return this.getContent();
    }

    getContent() {
        let meta = Object.assign({}, this.state.meta);
        let {links, bodyClassName, containerType } = this.state;

        return (
        	<Page width={containerType}>
				<div className='legacy-page'>
					<Meta meta={meta} links={links} bodyClassName={bodyClassName}/>
					<Content body={this.state.body} />
				</div>
			</Page>
        )
    }

    loadContentByClass() {
        const className = 'load-content';
        let elements = Array.from(document.body.getElementsByClassName(className));
        elements.forEach(e => {
            let url = e.dataset.url;
            fetch(url, {credentials: 'include'}).then(r => r.text()).then(data => {
                e.innerHTML = data;
                let scripts = Array.from(e.getElementsByTagName('script'));
                scripts.filter(s => ! s.src).forEach(s => safeEval(s.innerHTML));
            });
        });
    }

    setPostFormSubmitHandler() {
        let exceptions = ["/Login/ExternalLogin", "/Login/DoLinkLogin"];

        let forms = Array.from(document.body.getElementsByTagName('form'));
        let postForms = forms.filter(f => f.method.toLowerCase() === 'post' && !f.onsubmit && f.action);
        postForms.forEach(f => {
            let formUrl = f.action;
            if (exceptions.some(e => getUrlParts(formUrl).pathname.startsWith(e)))
                return;

            f.addEventListener('submit', e => {
				let formTarget = f.target;
				if (formTarget === '_blank')
					return true;

                e.preventDefault();

                /* Add button's data to form data */

                let formData = new FormData(f);
                let button = document.activeElement;
                if (button && button.name && button.value)
                    formData.append(button.name, button.value);

                fetch(formUrl, {
                    method: 'POST',
                    credentials: 'include',
                    body: formData
                }).then(response => {
                    if (response.redirected) {
                        /* If it was the login form, then update user information in header */
						let formUrlParts = getUrlParts(formUrl).pathname;
                        if (formUrlParts.startsWith('/Login') || formUrlParts.startsWith('/Account/') || formUrlParts.startsWith('/RestorePassword/'))
                            this.props.updateUserInformation();

						let newUrlParts = getUrlParts(response.url);

                        /* Is URL has not been changed then add random query parameter to enforce page reloading */
						let oldUrlParts = getUrlParts(window.location.href);
                        const isUrlChanged = oldUrlParts.pathname + oldUrlParts.search !== newUrlParts.pathname + newUrlParts.search;
                        if (! isUrlChanged)
							newUrlParts.search += (newUrlParts.search === '' ? '?' : '&') + 'rnd=' + Math.random();

                        this.context.router.history.replace(newUrlParts.pathname + newUrlParts.search);
                        return Promise.resolve(undefined);
                    }
                    return response.text()
                }).then(data => {
                    if (typeof data === 'undefined')
                        return;
                    this.processNewHtmlContent(formUrl, data)
                })
            });
        });
    }

    static mapStateToProps(state) {
        return {
            // For reloading page after logging out or changing current user information
            account: state.account
        };
    }

    static mapDispatchToProps(dispatch) {
        return {
            enterToCourse: (courseId) => dispatch({
                type: 'COURSES__COURSE_ENTERED',
                courseId: courseId
            }),
            updateUserInformation: () => dispatch(api.account.getCurrentUser())
        }
    }

    static contextTypes = {
        router: PropTypes.shape({
            history: PropTypes.shape({
                push: PropTypes.func.isRequired,
                replace: PropTypes.func.isRequired,
                createHref: PropTypes.func.isRequired
            }).isRequired
        }).isRequired
    };
}

class Content extends PureComponent {
    render() {
        return (<div dangerouslySetInnerHTML={{__html: this.props.body}}/>)
    }
}

class Meta extends Component {
    render() {
        let meta = this.props.meta;
        let links = this.props.links;
        let bodyClassName = this.props.bodyClassName;
        let renderedLinks = [];
        for (let i = 0; i < links.length; i++) {
            let link = links[i];
            renderedLinks.push(<link rel={link.rel} type={link.type} href={link.href} key={i}/>);
        }
        meta.title = decodeHtmlEntities(meta.title);
        meta.description = decodeHtmlEntities(meta.description);
        meta.keywords = decodeHtmlEntities(meta.keywords);
        return (
            <Helmet defer={false}>
                <title>{ meta.title }</title>
                <meta name="title" content={ meta.title }/>
                <meta property="og:title" content={ meta.title }/>
                <meta property="og:image" content={ meta.imageUrl }/>
                <meta property="og:image:alt" content={ meta.description }/>
                <meta property="og:description" content={ meta.description }/>
                <meta property="og:locale" content="ru_RU"/>
                <meta property="og:site_name" content="Ulearn"/>
                <meta name="description" content={ meta.description }/>
                <meta name="keywords" content={ meta.keywords }/>
                <link rel="image_src" href={ meta.imageUrl }/>
				<body className={ bodyClassName }/>
                {renderedLinks}
            </Helmet>
        )
    }
}

export default connect(DownloadedHtmlContent.mapStateToProps, DownloadedHtmlContent.mapDispatchToProps)(DownloadedHtmlContent);