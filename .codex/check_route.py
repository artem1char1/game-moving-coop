"""Local delegation preflight; not an API token quota or tool interceptor."""
import argparse,json,pathlib,sys
p=argparse.ArgumentParser()
p.add_argument('--class',dest='kind',choices=['S','M','L','C'],required=True)
p.add_argument('--role',action='append',default=[])
p.add_argument('--calls-used',type=int,default=0)
p.add_argument('--reason',default='')
a=p.parse_args()
policy=json.loads((pathlib.Path(__file__).parent.parent/'docs/operations/agent-policy.json').read_text(encoding='utf-8-sig'))
r=policy['routing'][a.kind]; errors=[]
if a.calls_used<0: errors.append('calls-used must be nonnegative')
if a.role and not a.reason.strip(): errors.append('Concrete delegation reason required')
if len(a.role)+a.calls_used>r['max_subagent_calls']: errors.append('Routing budget exceeded; reduce roles or record justified policy extension')
if any(x not in ['project_manager','gamedev_developer','qa_reviewer'] for x in a.role): errors.append('Unknown role')
if len(a.role)>1: errors.append('Delegate one bounded result at a time, not a default team')
print(json.dumps({'allowed':not errors,'class':a.kind,'roles':a.role,'planned_total':a.calls_used+len(a.role),'limit':r['max_subagent_calls'],'errors':errors},ensure_ascii=False))
sys.exit(2 if errors else 0)
